using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DxLabCoworkingSpace
{
    public class AreaTypeCategoryForAddDTO
    {
        [Required(ErrorMessage = "Bạn bắt buộc nhập tiêu đề của loại dịch vụ!")]
        [StringLength(100, ErrorMessage = "Tiêu đề không được vượt quá 100 ký tự!")]
        [RegularExpression(@"^[a-zA-Z0-9\sÀ-ỹđĐ]+$", ErrorMessage = "Tiêu đề không được chứa ký tự đặc biệt!")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Bạn bắt buộc nhập mô tả cho loại dịch vụ!")]
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự!")]
        public string CategoryDescription { get; set; }

        [Required(ErrorMessage = "Bạn phải tải lên ít nhất 1 hình ảnh!")]
        [MinLength(1, ErrorMessage = "Bạn phải tải lên ít nhất 1 hình ảnh!")]
        public List<IFormFile> Images { get; set; }

    }
}
