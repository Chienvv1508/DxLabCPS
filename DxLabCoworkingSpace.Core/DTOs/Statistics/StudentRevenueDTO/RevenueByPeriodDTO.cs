using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace.Core
{
    public class RevenueByPeriodDTO
    {
        public int PeriodNumber { get; set; } // Số thứ tự tháng (1-12) hoặc ngày (1-31)
        public StudentRevenueDTO Revenue { get; set; }
    }
}
