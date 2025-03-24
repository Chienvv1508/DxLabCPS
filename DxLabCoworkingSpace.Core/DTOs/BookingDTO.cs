using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class BookingDTO
    {
        [Required(ErrorMessage ="Bắt buộc nhập room")]
        public int RoomId { get; set; }
        [Required(ErrorMessage ="Bắt buộc nhập loại khu vực")] 
        public int AreaTypeId { get; set; }
        [Required(ErrorMessage ="Bắt buộc nhập thời gian đặt phòng")]
        public List<BookingTime> bookingTimes { get; set; }



    }
    public class BookingTime
    {
        [Required(ErrorMessage ="Bắt buộc nhập ngày đặt phòng")]
        public DateTime BookingDate { get; set; }
        [Required(ErrorMessage ="Bắt buộc nhập slot")]
        public List<int> SlotId { get; set; }
    }
}
