using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface ILabBookingCrawlerService
    {
        Task CrawlBookingEventsAsync(int fromBlock, int toBlock);
        Task<int?> GetDeploymentBlockAsync(string transactionHash);
        Task<int?> GetLatestBlockNumberAsync();
    }
}
