using System;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<string, DateTime> _mintedUsers = new ConcurrentDictionary<string, DateTime>();

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
                ?? "https://sepolia.infura.io/v3/ce5f177778e547a19055596b216fd743";

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

            var account = new Nethereum.Web3.Accounts.Account(_privateKey, 11155111);
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
                "*/5 * * * *",
                TimeZoneInfo.Local
            );

            RecurringJob.AddOrUpdate(
                "minting-job",
                () => ExecuteMintingJob(),
                "0 0 * * *",
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

            DateTime currentTime = DateTime.UtcNow.AddHours(7);

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var usersQuery = await unitOfWork.UserRepository.GetAll(u =>
                u.Status == true &&
                !string.IsNullOrEmpty(u.WalletAddress) &&
                u.RoleId == 3);

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

            int eligibleUsers = 0;
            foreach (var user in users)
            {
                bool shouldMint = false;
                if (_mintedUsers.TryGetValue(user.WalletAddress, out var lastMintedTime))
                {
                    if ((currentTime - lastMintedTime).TotalHours >= 24)
                    {
                        shouldMint = true;
                        Console.WriteLine($"Minting tokens for {user.WalletAddress}: Last minted at {lastMintedTime}, now eligible.");
                    }
                    else
                    {
                        Console.WriteLine($"Mint skipped for {user.WalletAddress}: Last minted at {lastMintedTime}, too soon.");
                    }
                }
                else
                {
                    shouldMint = true;
                    Console.WriteLine($"First time minting for {user.WalletAddress}");
                }

                if (shouldMint)
                {
                    eligibleUsers++;
                    var (mintSuccess, txHash) = await MintTokenForUser(user.WalletAddress);
                    if (mintSuccess)
                    {
                        _mintedUsers[user.WalletAddress] = currentTime;
                        Console.WriteLine($"Updated mint time for {user.WalletAddress} to {currentTime}");
                    }
                    else if (txHash != null)
                    {
                        Console.WriteLine($"Transaction sent but not confirmed yet for {user.WalletAddress}: {txHash}. Please check the transaction status on Sepolia testnet.");
                    }
                    else
                    {
                        Console.WriteLine($"Mint failed for {user.WalletAddress} after all retries.");
                    }
                }
                await Task.Delay(30000);
            }

            Console.WriteLine($"[MintingJob] End at {DateTime.UtcNow}, {eligibleUsers} users were eligible for minting.");
        }

        public async Task<(bool success, string txHash)> MintTokenForUser(string walletAddress)
        {
            if (!AddressUtil.Current.IsValidEthereumAddressHexFormat(walletAddress))
            {
                Console.WriteLine($"Invalid wallet address: {walletAddress}");
                return (false, null);
            }

            const int maxRetries = 5;
            var amountToMint = Nethereum.Util.UnitConversion.Convert.ToWei(100m);
            var mintFunction = _contract.GetFunction("mintForUser");

            // Lấy nonce hiện tại từ mạng
            HexBigInteger nonce = null;
            for (int retry = 1; retry <= maxRetries; retry++)
            {
                try
                {
                    nonce = await _web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(
                        _web3.TransactionManager.Account.Address,
                        BlockParameter.CreatePending()
                    );
                    Console.WriteLine($"Current nonce for {_web3.TransactionManager.Account.Address}: {nonce.Value}");
                    break;
                }
                catch (RpcResponseException rpcEx)
                {
                    Console.WriteLine($"RPC Error (retry {retry}/{maxRetries}) for eth_getTransactionCount: {rpcEx.Message}");
                    if (rpcEx.Message.Contains("429"))
                    {
                        await Task.Delay(10000 * retry);
                        if (retry == maxRetries)
                        {
                            Console.WriteLine("Max retries reached for eth_getTransactionCount due to rate limit. Aborting minting.");
                            return (false, null);
                        }
                    }
                    else
                    {
                        if (retry == maxRetries)
                        {
                            Console.WriteLine("Max retries reached for eth_getTransactionCount. Aborting minting.");
                            return (false, null);
                        }
                        await Task.Delay(5000);
                    }
                }
            }

            if (nonce == null)
            {
                Console.WriteLine("Failed to retrieve nonce. Aborting minting.");
                return (false, null);
            }

            // Kiểm tra chain ID
            var chainId = new HexBigInteger(0);
            for (int retry = 1; retry <= maxRetries; retry++)
            {
                try
                {
                    chainId = await _web3.Eth.ChainId.SendRequestAsync();
                    Console.WriteLine($"Current chain ID: {chainId}");
                    break;
                }
                catch (RpcResponseException rpcEx)
                {
                    Console.WriteLine($"RPC Error (retry {retry}/{maxRetries}) for eth_chainId: {rpcEx.Message}");
                    if (rpcEx.Message.Contains("429"))
                    {
                        await Task.Delay(10000 * retry);
                        if (retry == maxRetries)
                        {
                            Console.WriteLine("Max retries reached for eth_chainId due to rate limit. Aborting minting.");
                            return (false, null);
                        }
                    }
                    else
                    {
                        if (retry == maxRetries)
                        {
                            Console.WriteLine("Max retries reached for eth_chainId. Aborting minting.");
                            return (false, null);
                        }
                        await Task.Delay(5000);
                    }
                }
            }

            if (chainId.Value != 11155111)
            {
                Console.WriteLine($"Chain ID mismatch! Expected 11155111 (Sepolia), but got {chainId.Value}. Aborting minting.");
                return (false, null);
            }

            // Kiểm tra số dư ETH
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
                    if (rpcEx.Message.Contains("429"))
                    {
                        await Task.Delay(10000 * retry);
                        if (retry == maxRetries)
                        {
                            Console.WriteLine("Max retries reached for eth_getBalance due to rate limit. Aborting minting.");
                            return (false, null);
                        }
                    }
                    else
                    {
                        if (retry == maxRetries)
                        {
                            Console.WriteLine("Max retries reached for eth_getBalance. Aborting minting.");
                            return (false, null);
                        }
                        await Task.Delay(5000);
                    }
                }
            }

            if (ethBalance < 0.01m)
            {
                Console.WriteLine("Insufficient ETH balance for transactions!");
                return (false, null);
            }

            bool minted = false;
            string txHash = null;
            for (int retry = 1; retry <= maxRetries && !minted; retry++)
            {
                try
                {
                    Console.WriteLine($"Processing mint for {walletAddress} (attempt {retry}/{maxRetries})...");

                    var gasEstimate = await mintFunction.EstimateGasAsync(walletAddress, amountToMint);
                    var gasLimit = new HexBigInteger(gasEstimate.Value * 120 / 100);
                    var gasPrice = new HexBigInteger(15000000000); // 15 Gwei

                    txHash = await mintFunction.SendTransactionAsync(
                        _web3.TransactionManager.Account.Address,
                        gasLimit,
                        gasPrice,
                        walletAddress,
                        amountToMint,
                        nonce);

                    Console.WriteLine($"Transaction sent for {walletAddress}: {txHash}");

                    var receipt = await WaitForReceipt(_web3, txHash);
                    if (receipt?.Status.Value == 1)
                    {
                        Console.WriteLine($"Minted successfully for {walletAddress}");
                        minted = true;
                    }
                    else if (receipt == null)
                    {
                        Console.WriteLine($"Could not get receipt for {walletAddress}. Transaction may still be pending.");
                        return (false, txHash);
                    }
                    else
                    {
                        Console.WriteLine($"Mint failed for {walletAddress}: Status = 0");
                        return (false, null);
                    }
                }
                catch (SmartContractRevertException revertEx)
                {
                    Console.WriteLine($"REVERT for {walletAddress}: {revertEx.RevertMessage}");
                    return (false, null);
                }
                catch (RpcResponseException rpcEx)
                {
                    Console.WriteLine($"RPC Error for {walletAddress} (retry {retry}/{maxRetries}): {rpcEx.Message}");
                    if (rpcEx.Message.Contains("429"))
                    {
                        await Task.Delay(10000 * retry);
                        if (retry == maxRetries)
                        {
                            Console.WriteLine($"Max retries reached for {walletAddress} due to rate limit. Transaction may still be pending.");
                            return (false, txHash);
                        }
                    }
                    else
                    {
                        if (retry == maxRetries)
                        {
                            Console.WriteLine($"Max retries reached for {walletAddress}. Skipping.");
                            return (false, null);
                        }
                        await Task.Delay(5000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error for {walletAddress} (retry {retry}/{maxRetries}): {ex.Message}");
                    if (retry == maxRetries)
                    {
                        Console.WriteLine($"Max retries reached for {walletAddress}. Skipping.");
                        return (false, null);
                    }
                    await Task.Delay(5000);
                }
            }

            return (minted, txHash);
        }

        private async Task<TransactionReceipt> WaitForReceipt(Web3 web3, string transactionHash)
        {
            if (string.IsNullOrEmpty(transactionHash))
                return null;

            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            int attempts = 0;
            const int maxAttempts = 24;
            int rateLimitCount = 0;
            const int maxRateLimitRetries = 3;

            while (receipt == null && attempts < maxAttempts)
            {
                await Task.Delay(5000);
                try
                {
                    receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                    rateLimitCount = 0;
                }
                catch (RpcResponseException rpcEx)
                {
                    if (rpcEx.Message.Contains("429"))
                    {
                        rateLimitCount++;
                        if (rateLimitCount >= maxRateLimitRetries)
                        {
                            Console.WriteLine($"Too many rate limit errors (429) for transaction {transactionHash}. Stopping attempts.");
                            return null;
                        }
                        await Task.Delay(10000 * rateLimitCount);
                    }
                }
                attempts++;
            }

            if (receipt == null)
            {
                Console.WriteLine($"Failed to get receipt for transaction {transactionHash} after {maxAttempts} attempts.");
            }

            return receipt;
        }

        public async Task<string> CancelPendingTransaction(string txHashToCancel)
        {
            try
            {
                var pendingTx = await _web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txHashToCancel);
                if (pendingTx == null)
                {
                    Console.WriteLine($"Transaction {txHashToCancel} not found or already processed.");
                    return null;
                }

                var nonce = pendingTx.Nonce;
                var gasPrice = new HexBigInteger(15000000000); // 15 Gwei
                var gasLimit = new HexBigInteger(21000);

                var txInput = new TransactionInput
                {
                    From = _web3.TransactionManager.Account.Address,
                    To = _web3.TransactionManager.Account.Address,
                    Value = new HexBigInteger(0),
                    GasPrice = gasPrice,
                    Gas = gasLimit,
                    Nonce = nonce
                };

                var signedTx = await _web3.TransactionManager.SignTransactionAsync(txInput);
                var cancelTxHash = await _web3.Eth.Transactions.SendRawTransaction.SendRequestAsync("0x" + signedTx);

                Console.WriteLine($"Cancel transaction sent for {txHashToCancel}: {cancelTxHash}");
                return cancelTxHash;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cancelling transaction {txHashToCancel}: {ex.Message}");
                return null;
            }
        }
    }

    public class ContractAddresses
    {
        public string Booking { get; set; }
        public string DXLABCoin { get; set; }
    }
}