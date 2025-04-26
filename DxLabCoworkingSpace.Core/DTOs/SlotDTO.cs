using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class SlotDTO
    {
        public int SlotId { get; set; }

        [Required(ErrorMessage = "StartTime là bắt buộc!")]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$", ErrorMessage = "StartTime sai định dạng thời gian!")]
        public TimeSpan? StartTime { get; set; }

        [Required(ErrorMessage = "EndTime là bắt buộc!")]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$", ErrorMessage = "EndTime sai định dang thời gian!")]
        public TimeSpan? EndTime { get; set; }
        //public int Status { get; set; }

        public DateTime ExpiredTime { get; set; }


        public int SlotNumber { get; set; }
    }
}
