using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IStatisticsService
    {
        Task<StudentRevenueDTO> GetRevenueByStudentGroup(string period, int? year = null, int? month = null, int? week = null);
    }
}
