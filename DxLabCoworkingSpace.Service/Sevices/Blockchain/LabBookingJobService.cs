using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.Model;
using Nethereum.RPC.Eth.DTOs;
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
            _contractAddress = _configuration.GetSection("ContractAddresses:Sepolia")["FPTCurrency"]
                ?? throw new ArgumentNullException("ContractAddresses:Sepolia:FPTCurrency not configured");
            _sepoliaRpcUrl = _configuration.GetSection("Network")["ProviderCrawl"]
                ?? "https://sepolia.infura.io/v3/027867a8ebd44bc192e7f7b33baf4b4e";
            string labBookingPath = Path.Combine(Directory.GetCurrentDirectory(), "Contracts", "LabBookingSystem.json");
            string fptPath = Path.Combine(Directory.GetCurrentDirectory(), "Contracts", "FPTCurrency.json");

            if (!File.Exists(labBookingPath))
                throw new FileNotFoundException($"LabBookingSystem ABI file not found at {labBookingPath}");
            if (!File.Exists(fptPath))
                throw new FileNotFoundException($"FPTCurrency ABI file not found at {fptPath}");

            // Đọc và trích xuất ABI từ file
            var labBookingJson = File.ReadAllText(labBookingPath);
            var fptJson = File.ReadAllText(fptPath);

            // Parse JSON và lấy phần abi
            using var labBookingDoc = JsonDocument.Parse(labBookingJson);
            using var fptDoc = JsonDocument.Parse(fptJson);
            _labBookingContractAbi = labBookingDoc.RootElement.GetProperty("abi").GetRawText();
            _fptContractAbi = fptDoc.RootElement.GetProperty("abi").GetRawText();
        }
        public void ScheduleJob() 
        {
            //RecurringJob.AddOrUpdate(
            //    "booking-log-job",
            //    () => RunBookingLogJobAsync(),
            //    "*/30 * * * * *", // Chạy mỗi 30 giây
            //    TimeZoneInfo.FindSystemTimeZoneById("America/New_York")
            //);
            //Console.WriteLine("Crawl job was scheduled!");

            //RecurringJob.AddOrUpdate(
            //    "minting-job",
            //    () => ExecuteMintingJob(),
            //    "0 0 * * *", // Chạy hàng ngày lúc 0h (nửa đêm) EST
            //    TimeZoneInfo.FindSystemTimeZoneById("America/New_York")
            //);
            //Console.WriteLine("Minting job was scheduled!");
         
            RecurringJob.AddOrUpdate(
                "booking-log-job",
                () => RunBookingLogJobAsync(),
                "*/5 * * * * *", 
                TimeZoneInfo.Local
            );

            RecurringJob.AddOrUpdate(
                "minting-job",
                () => ExecuteMintingJob(),
                "*/5 * * * * *", 
                TimeZoneInfo.Local
            );

            Console.WriteLine("Jobs were scheduled!");

            BackgroundJob.Enqueue(() => RunBookingLogJobAsync());
            BackgroundJob.Enqueue(() => ExecuteMintingJob());
        }

        public async Task RunBookingLogJobAsync()
        {
            Console.WriteLine("Starting to crawl booking logs...");

            try
            {
                var lastBlockRecord = await _unitOfWork.ContractCrawlRepository.Get(c => c.ContractName == "LabBookingSystem");
                int fromBlock = lastBlockRecord != null ? int.Parse(lastBlockRecord.LastBlock) : 0;

                var latestBlock = await _crawler.GetLatestBlockNumberAsync();

                if (!latestBlock.HasValue)
                {
                    Console.WriteLine("Could not get latest block.");
                    return;
                }

                int toBlock = (int)latestBlock.Value;
                const int batchSize = 500;
                const int maxRetry = 3;

                for (int i = fromBlock; i <= toBlock; i += batchSize)
                {
                    int batchFrom = i;
                    int batchTo = Math.Min(i + batchSize - 1, toBlock);

                    Console.WriteLine($"Crawling from block {batchFrom} to {batchTo}...");

                    for (int retry = 1; retry <= maxRetry; retry++)
                    {
                        try
                        {
                            await _crawler.CrawlBookingEventsAsync(batchFrom, batchTo);

                            // Cập nhật last block
                            var contractCrawl = await _unitOfWork.ContractCrawlRepository.Get(c => c.ContractName == "LabBookingSystem");
                            if (contractCrawl != null)
                            {
                                contractCrawl.LastBlock = batchTo.ToString();
                                await _unitOfWork.CommitAsync();
                                Console.WriteLine($"Update LastBlock: {batchTo}");
                            }
                            break; // Thành công, thoát retry loop
                        }
                        catch (RpcResponseException rpcEx)
                        {
                            Console.WriteLine($"RPC error (retry {retry}/{maxRetry}) for blocks {batchFrom}-{batchTo}: {rpcEx.Message}");
                            if (retry == maxRetry)
                            {
                                Console.WriteLine($"Skip batch {batchFrom}-{batchTo} after {maxRetry} trys.");
                            }
                            await Task.Delay(1000);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Unexpected error from block {batchFrom} to {batchTo}: {ex.Message}");
                            break;
                        }
                    }
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
            Console.WriteLine($"Starting daily minting job at {DateTime.UtcNow}");

            Console.WriteLine($"PrivateKey: {_privateKey}");
            var account = new Nethereum.Web3.Accounts.Account(_privateKey);
            Console.WriteLine($"AccountAddress: {account.Address}");
            var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var client = new RpcClient(new Uri(_sepoliaRpcUrl), httpClient);
            var web3 = new Web3(account, client);

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();


            var users = await unitOfWork.UserRepository.GetAll(u => u.Status == true && !string.IsNullOrEmpty(u.WalletAddress));


            var contract = web3.Eth.GetContract(_fptContractAbi, _contractAddress);
            var mintFunction = contract.GetFunction("mintForUser");
            var amountToMint = Nethereum.Util.UnitConversion.Convert.ToWei(100m);

            foreach (var user in users)
            {
                try
                {
                    var gasEstimate = await mintFunction.EstimateGasAsync(user.WalletAddress, amountToMint);
                    var transactionHash = await mintFunction.SendTransactionAsync(
                        from: account.Address,
                        gas: gasEstimate,
                        value: new HexBigInteger(0),
                        user.WalletAddress,
                        amountToMint
                    );

                    Console.WriteLine($"Mint transaction sent for user {user.WalletAddress}, TxHash: {transactionHash}");

                    var receipt = await WaitForReceipt(web3, transactionHash);
                    if (receipt?.Status.Value == 1)
                    {
                        Console.WriteLine($"Mint successful for user {user.WalletAddress}");
                    }
                    else
                        Console.WriteLine($"Mint failed for user {user.WalletAddress}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error minting for user {user.WalletAddress}: {ex.Message}");
                }
            }

            Console.WriteLine($"Completed daily minting job at {DateTime.UtcNow}");
        }

        public async Task<TransactionReceipt> WaitForReceipt(Web3 web3, string transactionHash)
        {
            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            int attempts = 0;
            const int maxAttempts = 30;
                
            while (receipt == null && attempts < maxAttempts)
            {
                await Task.Delay(1000);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                attempts++;
            }

            return receipt;
        }
    }

    public class ContractAddresses
    {
        public string LabBookingSystem { get; set; }
        public string FPTCurrency { get; set; }
    }
}