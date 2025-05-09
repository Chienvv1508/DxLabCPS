﻿using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class BlogDTO
    {
        public enum BlogStatus
        {
            Cancel = 0,
            Pending = 1,
            Approve = 2
        }

        public int BlogId { get; set; }

        [Required(ErrorMessage = "Tiêu đề blog là bắt buộc")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Tiêu đề blog phải từ 5 đến 50 ký tự")]
        public string BlogTitle { get; set; } = null!;

        [Required(ErrorMessage = "Nội dung blog là bắt buộc")]
        [StringLength(int.MaxValue, MinimumLength = 10, ErrorMessage = "Nội dung blog phải ít nhất 10 ký tự")]
        public string BlogContent { get; set; } = null!;

        [DefaultValue("")]
        public String BlogCreatedDate { get; set; }

        public BlogStatus Status { get; set; }
        public string? UserName { get; set; }
        public List<IFormFile>? ImageFiles { get; set; }
        public List<string>? Images { get; set; } // list đường dẫn ảnh trong response
    }
}
