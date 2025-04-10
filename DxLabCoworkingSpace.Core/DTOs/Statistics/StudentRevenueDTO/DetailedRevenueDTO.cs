using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace.Core
{
    public class DetailedRevenueDTO
    {
        public string Period { get; set; } // "year" hoặc "month"
        public int Year { get; set; }
        public int? Month { get; set; } // Null nếu là year
        public List<RevenueByPeriodDTO> Details { get; set; }
    }
}
