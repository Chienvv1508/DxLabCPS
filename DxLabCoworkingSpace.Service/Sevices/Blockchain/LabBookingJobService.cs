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
        public void ScheduleBookingLogJob()
        {
            RecurringJob.AddOrUpdate(
                "booking-log-job", // ID của job
                () => RunBookingLogJobAsync(), // Phương thức chạy job
                "*/5 * * * * *", // Cron expression: mỗi 15 giây
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


                //if (lastBlockRecord == null)
                //{
                //    fromBlock = 0; // Bắt đầu từ block 0
                //    await _unitOfWork.ContractCrawlRepository.Add(new ContractCrawl
                //    {
                //        ContractName = "LabBookingSystem",
                //        ContractAddress = contractAddresses.LabBookingSystem,
                //        LastBlock = fromBlock.ToString()
                //    });
                //    await _unitOfWork.CommitAsync();
                //}
                //else
                //{
                //    fromBlock = int.Parse(lastBlockRecord.LastBlock);
                //}


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