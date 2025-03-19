using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace.Core.DTOs
{
    public class BlogRequestDTO
    {
        [Required(ErrorMessage = "Tiêu đề blog là bắt buộc")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Tiêu đề blog phải từ 5 đến 50 ký tự")]
        public string BlogTitle { get; set; } = null!;

        [Required(ErrorMessage = "Nội dung blog là bắt buộc")]
        [StringLength(int.MaxValue, MinimumLength = 10, ErrorMessage = "Nội dung blog phải ít nhất 10 ký tự")]
        public string BlogContent { get; set; } = null!;

        public List<IFormFile>? ImageFiles { get; set; }
    }
}
