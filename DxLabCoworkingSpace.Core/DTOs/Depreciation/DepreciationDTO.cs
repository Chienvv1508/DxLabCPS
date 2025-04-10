using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class DepreciationDTO
    {
        public long DepreciationSumId { get; set; }

        public int? FacilityId { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime ImportDate { get; set; }

        public decimal DepreciationAmount { get; set; }
        public DateTime SumDate { get; set; }

        public string? FacilityTitle { get; set; }
        public int FacilityCategory { get; set; }
    }
}
