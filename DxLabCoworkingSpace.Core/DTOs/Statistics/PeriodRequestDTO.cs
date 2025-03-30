using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class PeriodRequestDTO
    {
        [Required(ErrorMessage = "Period là bắt buộc!")]
        [RegularExpression("^(tuần|tháng|năm)$", ErrorMessage = "Period không hợp lệ, phải là 'tuần', 'tháng' hoặc 'năm'!")]
        public string Period { get; set; }

        [Range(2000, int.MaxValue, ErrorMessage = "Năm không hợp lệ!")]
        public int? Year { get; set; }

        [Range(1, 12, ErrorMessage = "Tháng phải từ 1 đến 12!")]
        public int? Month { get; set; }

        [Range(1, 5, ErrorMessage = "Tuần phải từ 1 đến 5!")]
        public int? Week { get; set; }
    }
}
