using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.Configuration;

namespace DxLabCoworkingSpace
{
    public class LabBookingJobService : ILabBookingJobService
    {
        private readonly ILabBookingCrawlerService _crawler;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration; // Thêm IConfiguration
        private readonly string _deploymentsPath = "Contracts/deployments.json";
        private readonly string _labBookingSystemAbiPath = "Contracts/LabBookingSystem.json";

        public LabBookingJobService(IUnitOfWork unitOfWork, ILabBookingCrawlerService crawler, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _crawler = crawler;
            _configuration = configuration;
        }

        public void ScheduleBookingLogJob()
        {
            RecurringJob.AddOrUpdate("booking-log-job", () => RunBookingLogJobAsync(), "*/15 * * * * *", TimeZoneInfo.FindSystemTimeZoneById("America/New_York"));
        }

        public async Task RunBookingLogJobAsync()
        {
            Console.WriteLine("Starting to crawl booking logs...");
            try
            {
                var lastBlockRecord = await _unitOfWork.ContractCrawlRepository.Get(c => c.ContractName == "LabBookingSystem");

                // Đọc ContractAddresses từ appsettings.json
                var contractAddresses = _configuration.GetSection("ContractAddresses:Sepolia").Get<ContractAddresses>();

                var deploymentsJson = File.ReadAllText(_deploymentsPath);
                var deployments = JsonSerializer.Deserialize<Dictionary<string, Deployment>>(deploymentsJson);

                int fromBlock = 0;

                if (lastBlockRecord == null)
                {
                    fromBlock = (await _crawler.GetDeploymentBlockAsync(deployments["sepolia"].TransactionHash))
                        ?? deployments["sepolia"].BlockNumber;
                    await _unitOfWork.ContractCrawlRepository.Add(new ContractCrawl
                    {
                        ContractName = "LabBookingSystem",
                        ContractAddress = contractAddresses.LabBookingSystem,
                        LastBlock = fromBlock.ToString()
                    });
                    await _unitOfWork.CommitAsync();
                }
                else if (int.Parse(lastBlockRecord.LastBlock) < deployments["sepolia"].BlockNumber)
                {
                    fromBlock = deployments["sepolia"].BlockNumber;
                    lastBlockRecord.LastBlock = fromBlock.ToString();
                    await _unitOfWork.CommitAsync();
                }
                else
                {
                    fromBlock = int.Parse(lastBlockRecord.LastBlock);
                }

                var latestBlock = await _crawler.GetLatestBlockNumberAsync();
                if (latestBlock.HasValue)
                {
                    await _crawler.CrawlBookingEventsAsync(fromBlock, latestBlock.Value);
                    var contractCrawl = await _unitOfWork.ContractCrawlRepository.Get(c => c.ContractName == "LabBookingSystem");
                    if (contractCrawl != null)
                    {
                        contractCrawl.LastBlock = latestBlock.Value.ToString();
                        await _unitOfWork.CommitAsync();
                    }
                }
            }
            catch (Exception error)
            {
                Console.WriteLine($"Error occurred while crawling booking logs: {error.Message}");
            }
        }
    }

    public class ContractAddresses
    {
        public string LabBookingSystem { get; set; }
        public string FPTCurrency { get; set; }
    }

    public class Deployment
    {
        public string LabBookingSystem { get; set; }
        public string TransactionHash { get; set; }
        public int BlockNumber { get; set; }
    }
}
