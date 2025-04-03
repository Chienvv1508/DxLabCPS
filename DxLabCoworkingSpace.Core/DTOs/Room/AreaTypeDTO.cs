using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class AreaTypeDTO
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int AreaTypeId { get; set; }

        [Required(ErrorMessage = "Tên khu vực không được để trống.")]
        [StringLength(255, MinimumLength = 3, ErrorMessage = "Tên khu vực phải từ 3 đến 255 ký tự.")]
        public string AreaTypeName { get; set; } = null!;

        [Required(ErrorMessage = "Loại danh mục khu vực không được để trống.")]
        [Range(1, 2, ErrorMessage = "Loại danh mục khu vực chỉ có thể là cá nhân hoặc nhóm.")]
        public int AreaCategory { get; set; }

        [Required(ErrorMessage = "Mô tả khu vực không được để trống.")]
        [StringLength(255, MinimumLength = 3, ErrorMessage = "Mô tả khu vực phải từ 3 đến 255 ký tự.")]
        public string AreaDescription { get; set; } = null!;

        [Required(ErrorMessage = "Số chỗ trong khu vực không được để trống.")]
        [Range(0, 40, ErrorMessage = "Số chỗ trong khu vực phải lớn hơn 0.")]
        public int Size { get; set; }

        [Required(ErrorMessage = "Giá không được để trống.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Trạng thái xóa không được để trống.")]
        public bool IsDeleted { get; set; }

        public List<string>? Images { get; set; }
    }
}
