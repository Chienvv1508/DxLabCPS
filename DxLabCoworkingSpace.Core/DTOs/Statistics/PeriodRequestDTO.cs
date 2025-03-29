using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class PeriodRequestDTO
    {
        public string Period { get; set; } // "tuần", "tháng", "năm"
        public int? Year { get; set; }     // Năm cụ thể
        public int? Month { get; set; }    // Tháng cụ thể 
        public int? Week { get; set; }     // Tuần cụ thể 
    }
}
