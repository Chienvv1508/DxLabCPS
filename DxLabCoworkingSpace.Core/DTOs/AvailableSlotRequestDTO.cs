using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class AvailableSlotRequestDTO
    {
        [Required(ErrorMessage = "Bạn bắt buộc nhập Room")]
        public int RoomId { get; set; }
        [Required(ErrorMessage = "Bạn bắt buộc nhập kiểu phòng")] 
        
        public int AreaTypeId { get; set; }
        [Required(ErrorMessage = "Bạn bắt buộc nhập ngày muốn đặt chỗ")]
        public DateTime BookingDate { get; set; }
    }
}
