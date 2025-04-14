using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class SlotGenerationRequest
    {
        [Required(ErrorMessage = "StartTime là bắt buộc!")]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$", ErrorMessage = "StartTime sai định dạng thời gian!")]
        public string? StartTime { get; set; }

        [Required(ErrorMessage = "EndTime là bắt buộc!")]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$", ErrorMessage = "EndTime sai định dạng thời gian!")]
        public string? EndTime { get; set; }

        [Required(ErrorMessage = "TimeSlot là bắt buộc!")]
        [Range(0, 150, ErrorMessage ="Thời gian 1 slot phải từ 0 đến 150 phút!")]
        public int? TimeSlot { get; set; }

        [Required(ErrorMessage = "BreakTime là bắt buộc!")]
        [Range(0, 20, ErrorMessage = "BreakTime phải là từ 0 đến 20 phút!")]
        public int? BreakTime { get; set; }
    }
}
