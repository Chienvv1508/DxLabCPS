using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class FaciAddDTO
    {
        [Required(ErrorMessage ="Bạn bắt buộc nhập thiết bị")]
        public int? FacilityId { get; set; }
        [Required(ErrorMessage ="Bạn bắt buộc nhập lô hàng")]
        public string? BatchNumber { get; set; }
        [Required(ErrorMessage = "Bắt buộc nhập ngày nhập")]
        public DateTime ImportDate { get; set; }
        [Required(ErrorMessage ="Bạn chưa nhập số lượng")]
        [Range(1,40,ErrorMessage ="Số lượng phải là số nguyên lớn hơn 1 và nhỏ hơn 40!")]
        public int Quantity { get; set; }
        
        

    }
}
