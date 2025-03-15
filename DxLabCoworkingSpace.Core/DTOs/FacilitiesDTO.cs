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
        [Required(ErrorMessage = "FacilityId không được để trống!")]
        public int FacilityId { get; set; }

        [Required(ErrorMessage = "BatchNumber không được để trống!")]
        [StringLength(50, ErrorMessage ="BatchNumber không quá 50 ký tự.")]
        public string BatchNumber { get; set; } = null!;
        public string? FacilityDescription { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Cost phải lớn hơn hoặc bằng 0")]
        public decimal Cost { get; set; }
        public DateTime ExpiredTime { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity phải lớn hơn hoặc bằng 0")]
        public int Quantity { get; set; }
        public DateTime ImportDate { get; set; }
    }
}
