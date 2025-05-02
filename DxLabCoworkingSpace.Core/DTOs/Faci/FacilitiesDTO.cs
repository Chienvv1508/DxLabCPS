using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class FacilitiesDTO
    {
        [Required(ErrorMessage = "FacilityId không được để trống!")]
        [Range(1000, 9999, ErrorMessage = "Mã sản phẩm phải nằm trong khoảng từ 1000 đến 9999.")]
        public int FacilityId { get; set; }

        [Required(ErrorMessage = "BatchNumber không được để trống!")]
        [StringLength(50, ErrorMessage ="BatchNumber không quá 50 ký tự.")]
        public string BatchNumber { get; set; } = null!;
        public string? FacilityTitle { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Cost phải lớn hơn 0")]
        public decimal Cost { get; set; }
        public DateTime ExpiredTime { get; set; }
        [Required(ErrorMessage = "Size không được để trống!")]
        [Range(0, int.MaxValue, ErrorMessage = "Size phải lớn hơn 0")]
        public int Size { get; set; }
        [Required(ErrorMessage = "Loại thiết bị không được để trống!")]
        [Range(0, 1, ErrorMessage = "Loại thiết bị chỉ chọn ghế hoặc bàn")]
        public int FacilityCategory { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity phải lớn hơn 0")]
        public int Quantity { get; set; }
        [Required(ErrorMessage = "Ngày nhập không được để trống!")]
        public DateTime ImportDate { get; set; }
        public decimal? RemainingValue { get; set; }

        //public List<FacilitiesStatus> FacilitiesStatus { get; set; }
    }
}
