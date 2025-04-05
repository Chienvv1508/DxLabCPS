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
        private readonly IConfiguration _configuration;

        public LabBookingJobService(IUnitOfWork unitOfWork, ILabBookingCrawlerService crawler, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _crawler = crawler;
            _configuration = configuration;
        }

        // Phương thức này sẽ được gọi khi ứng dụng khởi động để lập lịch job

        // tạo thêm job định kỳ 1 ngày gọi 1 lần 
        //  query tất cả account, gọi đến {
    //  "inputs": [
    //    {
    //      "internalType": "address",
    //      "name": "user",
    //      "type": "address"
    //    },
    //    {
    //      "internalType": "uint256",
    //      "name": "amount",
    //      "type": "uint256"
    //    }
    //  ],
    //  "name": "mintForUser",
    //  "outputs": [],
    //  "stateMutability": "nonpayable",
    //  "type": "function"
    //},
       
        public void ScheduleBookingLogJob()
        {
            RecurringJob.AddOrUpdate(
                "booking-log-job", // ID của job
                () => RunBookingLogJobAsync(), // Phương thức chạy job
                "*/30 * * * * *", // Cron expression: mỗi 30 giây
                TimeZoneInfo.FindSystemTimeZoneById("America/New_York") // Timezone
            );
            Console.WriteLine("Booking log job scheduled.");
        }

        public async Task RunBookingLogJobAsync()
        {
            Console.WriteLine("Starting to crawl booking logs...");
            try
            {
                var lastBlockRecord = await _unitOfWork.ContractCrawlRepository.Get(c => c.ContractName == "LabBookingSystem");
                var contractAddresses = _configuration.GetSection("ContractAddresses:Sepolia").Get<ContractAddresses>();

                int fromBlock = lastBlockRecord != null ? int.Parse(lastBlockRecord.LastBlock) : 0;
                var latestBlock = await _crawler.GetLatestBlockNumberAsync();
                Console.WriteLine($"{fromBlock}");
                Console.WriteLine($"{latestBlock}");
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
}