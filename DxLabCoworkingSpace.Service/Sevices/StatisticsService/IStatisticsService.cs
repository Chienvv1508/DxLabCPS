using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DxLabCoworkingSpace;
using DxLabCoworkingSpace.Core;

namespace DxLabCoworkingSpace
{
    public interface IStatisticsService
    {
        Task<DetailedRevenueDTO> GetDetailedRevenue(string period, int year, int? month = null);
    }
}
