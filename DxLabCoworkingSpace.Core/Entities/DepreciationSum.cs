using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class DepreciationSum
    {
        [Key]
        public long DepreciationSumId { get; set; }

        public int? FacilityId { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime ImportDate { get; set; }

        public decimal DepreciationAmount { get; set; }
        public DateTime SumDate { get; set; }

        public virtual Facility? Facility { get; set; }
    }
}
