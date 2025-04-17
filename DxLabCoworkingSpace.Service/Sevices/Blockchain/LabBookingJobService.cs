using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;

namespace DxLabCoworkingSpace
{
    public class LabBookingJobService : ILabBookingJobService
    {
        private readonly ILabBookingCrawlerService _crawler;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _privateKey;
        private readonly string _contractAddress;
        private readonly string _sepoliaRpcUrl;
        private readonly string _labBookingContractAbi;
        private readonly string _fptContractAbi;
        private Web3 _web3;
        private Contract _contract;

        public LabBookingJobService(
            ILabBookingCrawlerService crawler,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _crawler = crawler;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _privateKey = _configuration.GetSection("PrivateKeyBlockchain")["PRIVATE_KEY"]
                ?? throw new ArgumentNullException("PrivateKeyBlockchain:PRIVATE_KEY not configured");
            _contractAddress = _configuration.GetSection("ContractAddresses:Sepolia")["DXLABCoin"]
                ?? throw new ArgumentNullException("ContractAddresses:Sepolia:DXLABCoin not configured");
            _sepoliaRpcUrl = _configuration.GetSection("Network")["ProviderCrawl"]
                ?? "https://sepolia.infura.io/v3/9d13fab540c243ca9514d4ab4fe7e9e1";

            string labBookingPath = Path.Combine(Directory.GetCurrentDirectory(), "Contracts", "Booking.json");
            string fptPath = Path.Combine(Directory.GetCurrentDirectory(), "Contracts", "DXLABCoin.json");

            if (!File.Exists(labBookingPath))
                throw new FileNotFoundException($"Booking ABI file not found at {labBookingPath}");
            if (!File.Exists(fptPath))
                throw new FileNotFoundException($"DXLABCoin ABI file not found at {fptPath}");

            var labBookingJson = File.ReadAllText(labBookingPath);
            var fptJson = File.ReadAllText(fptPath);

            using var labBookingDoc = JsonDocument.Parse(labBookingJson);
            using var fptDoc = JsonDocument.Parse(fptJson);
            _labBookingContractAbi = labBookingDoc.RootElement.GetProperty("abi").GetRawText();
            _fptContractAbi = fptDoc.RootElement.GetProperty("abi").GetRawText();

            // Khởi tạo Web3 với chain ID
            var account = new Nethereum.Web3.Accounts.Account(_privateKey, 11155111); // Chain ID của Sepolia
            var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
            var client = new RpcClient(new Uri(_sepoliaRpcUrl), httpClient);
            _web3 = new Web3(account, client);
            _contract = _web3.Eth.GetContract(_fptContractAbi, _contractAddress);
        }

        public void ScheduleJob()
        {
            RecurringJob.AddOrUpdate(
                "booking-log-job",
                () => RunBookingLogJobAsync(),
                "*/5 * * * *", // Chạy mỗi 5 phút
                TimeZoneInfo.Local
            );

            RecurringJob.AddOrUpdate(
                "minting-job",
                () => ExecuteMintingJob(),
                "*/10 * * * *", // Chạy mỗi 10 phút
                TimeZoneInfo.Local
            );

            Console.WriteLine("Jobs were scheduled!");
        }

        public async Task RunBookingLogJobAsync()
        {
            Console.WriteLine("Starting to crawl booking logs...");

            try
            {
                var lastBlockRecord = await _unitOfWork.ContractCrawlRepository.Get(c => c.ContractName == "Booking");
                int fromBlock = lastBlockRecord != null ? int.Parse(lastBlockRecord.LastBlock) : 0;
                var latestBlock = await _crawler.GetLatestBlockNumberAsync();

                if (!latestBlock.HasValue)
                {
                    Console.WriteLine("Could not get latest block.");
                    return;
                }

                int toBlock = (int)latestBlock.Value;
                const int initialBatchSize = 2000000;
                const int maxRetry = 3;

                for (int i = fromBlock; i <= toBlock; i += initialBatchSize)
                {
                    int batchFrom = i;
                    int batchTo = Math.Min(i + initialBatchSize - 1, toBlock);

                    Console.WriteLine($"Crawling from block {batchFrom} to {batchTo}...");

                    for (int retry = 1; retry <= maxRetry; retry++)
                    {
                        try
                        {
                            await _crawler.CrawlBookingEventsAsync(batchFrom, batchTo);
                            break;
                        }
                        catch (RpcResponseException rpcEx)
                        {
                            Console.WriteLine($"RPC error (retry {retry}/{maxRetry}) for blocks {batchFrom}-{batchTo}: {rpcEx.Message}");
                            if (retry == maxRetry)
                            {
                                Console.WriteLine($"Skip batch {batchFrom}-{batchTo} after {maxRetry} tries.");
                            }
                            await Task.Delay(3000);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Unexpected error from block {batchFrom} to {batchTo}: {ex.Message}");
                            break;
                        }
                    }
                }

                var contractCrawl = await _unitOfWork.ContractCrawlRepository.Get(c => c.ContractName == "Booking");
                if (contractCrawl != null)
                {
                    contractCrawl.LastBlock = latestBlock.Value.ToString();
                    await _unitOfWork.CommitAsync();
                    Console.WriteLine($"Updated LastBlock: {latestBlock.Value}");
                }
                else
                {
                    await _unitOfWork.ContractCrawlRepository.Add(new ContractCrawl
                    {
                        ContractName = "Booking",
                        LastBlock = latestBlock.Value.ToString()
                    });
                    await _unitOfWork.CommitAsync();
                    Console.WriteLine($"Created new ContractCrawl with LastBlock: {latestBlock.Value}");
                }

                Console.WriteLine("Crawl successfully!");
            }
            catch (Exception error)
            {
                Console.WriteLine($"Error occurred while crawling booking logs: {error.Message}");
            }
        }

        public async Task ExecuteMintingJob()
        {
            Console.WriteLine($"Starting minting job at {DateTime.UtcNow}");

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var usersQuery = await unitOfWork.UserRepository.GetAll(u =>
                u.Status == true &&
                !string.IsNullOrEmpty(u.WalletAddress));

            var users = usersQuery
                .AsEnumerable()
                .Where(u => AddressUtil.Current.IsValidEthereumAddressHexFormat(u.WalletAddress))
                .ToList();

            Console.WriteLine($"Found {users.Count} users with valid wallets for minting.");

            if (!users.Any())
            {
                Console.WriteLine("No users with valid wallets to mint.");
                return;
            }

            foreach (var user in users)
            {
                await MintTokenForUser(user.WalletAddress);
                await Task.Delay(15000);
            }

            Console.WriteLine($"[MintingJob] End at {DateTime.UtcNow}");
        }

        public async Task<bool> MintTokenForUser(string walletAddress)
        {
            if (!AddressUtil.Current.IsValidEthereumAddressHexFormat(walletAddress))
            {
                Console.WriteLine($"Invalid wallet address: {walletAddress}");
                return false;
            }

            const int maxRetries = 5;
            var amountToMint = Nethereum.Util.UnitConversion.Convert.ToWei(100m);
            var mintFunction = _contract.GetFunction("mintForUser");

            // Kiểm tra chain ID
            var chainId = await _web3.Eth.ChainId.SendRequestAsync();
            Console.WriteLine($"Current chain ID: {chainId}");
            if (chainId.Value != 11155111)
            {
                Console.WriteLine($"Chain ID mismatch! Expected 11155111 (Sepolia), but got {chainId.Value}. Aborting minting.");
                return false;
            }

            decimal ethBalance = 0;
            for (int retry = 1; retry <= maxRetries; retry++)
            {
                try
                {
                    var balance = await _web3.Eth.GetBalance.SendRequestAsync(_web3.TransactionManager.Account.Address);
                    ethBalance = Nethereum.Util.UnitConversion.Convert.FromWei(balance.Value);
                    Console.WriteLine($"ETH Balance: {ethBalance} ETH");
                    break;
                }
                catch (RpcResponseException rpcEx)
                {
                    Console.WriteLine($"RPC Error (retry {retry}/{maxRetries}) for eth_getBalance: {rpcEx.Message}");
                    if (retry == maxRetries)
                    {
                        Console.WriteLine("Max retries reached for eth_getBalance. Aborting minting.");
                        return false;
                    }
                    await Task.Delay(3000);
                }
            }

            if (ethBalance < 0.01m)
            {
                Console.WriteLine("Insufficient ETH balance for transactions!");
                return false;
            }

            bool minted = false;
            string txHash = null;
            for (int retry = 1; retry <= maxRetries && !minted; retry++)
            {
                try
                {
                    Console.WriteLine($"Processing mint for {walletAddress} (attempt {retry}/{maxRetries})...");

                    var gasEstimate = await mintFunction.EstimateGasAsync(
                        _web3.TransactionManager.Account.Address,
                        null,
                        new HexBigInteger(0),
                        walletAddress,
                        amountToMint);

                    var gasLimit = new HexBigInteger(gasEstimate.Value * 120 / 100);
                    var gasPrice = new HexBigInteger(2000000000);

                    txHash = await mintFunction.SendTransactionAsync(
                        _web3.TransactionManager.Account.Address,
                        gasLimit,
                        gasPrice,
                        new HexBigInteger(0),
                        walletAddress,
                        amountToMint);

                    Console.WriteLine($"Transaction sent for {walletAddress}: {txHash}");

                    var receipt = await WaitForReceipt(_web3, txHash);
                    if (receipt?.Status.Value == 1)
                    {
                        Console.WriteLine($"Minted successfully for {walletAddress}");
                        minted = true;
                    }
                    else
                    {
                        Console.WriteLine($"Mint failed for {walletAddress}: {(receipt == null ? "No receipt" : "Status = 0")}");
                        return false;
                    }
                }
                catch (SmartContractRevertException revertEx)
                {
                    Console.WriteLine($"REVERT for {walletAddress}: {revertEx.RevertMessage}");
                    return false;
                }
                catch (RpcResponseException rpcEx)
                {
                    Console.WriteLine($"RPC Error for {walletAddress} (retry {retry}/{maxRetries}): {rpcEx.Message}");
                    if (retry == maxRetries)
                    {
                        Console.WriteLine($"Max retries reached for {walletAddress}. Skipping.");
                        return false;
                    }
                    await Task.Delay(3000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error for {walletAddress} (retry {retry}/{maxRetries}): {ex.Message}");
                    if (retry == maxRetries)
                    {
                        Console.WriteLine($"Max retries reached for {walletAddress}. Skipping.");
                        return false;
                    }
                    await Task.Delay(3000);
                }
            }

            return minted;
        }

        private async Task<TransactionReceipt> WaitForReceipt(Web3 web3, string transactionHash)
        {
            if (string.IsNullOrEmpty(transactionHash))
                return null;

            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            int attempts = 0;
            const int maxAttempts = 20;

            while (receipt == null && attempts < maxAttempts)
            {
                await Task.Delay(2000);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                attempts++;
            }

            return receipt;
        }
    }

    public class ContractAddresses
    {
        public string Booking { get; set; }
        public string DXLABCoin { get; set; }
    }
}