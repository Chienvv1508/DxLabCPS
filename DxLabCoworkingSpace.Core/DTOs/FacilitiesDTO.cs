using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace.Core.DTOs
{
    public class FacilitiesDTO
    {
        public int FacilityId { get; set; }
        public string BatchNumber { get; set; } = null!;
        public string? FacilityDescription { get; set; }
        [MinLength(0, ErrorMessage ="Cost phải lớn hơn 0")]
        public decimal Cost { get; set; }
        public DateTime ExpiredTime { get; set; }

        [MinLength(0, ErrorMessage ="Quantity phải lớn hơn 0")]
        public int Quantity { get; set; }
        public DateTime ImportDate { get; set; }
    }
}
