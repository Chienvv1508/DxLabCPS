using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class AreaAddDTO
    {
        [Required(ErrorMessage = "Bạn chưa nhập loại khu vực khi tạo phòng")]
        public int AreaTypeId { get; set; }
        [Required(ErrorMessage = "Bạn chưa nhập tên khu vực")]
        public string AreaTypeName { get; set; }
        [Required(ErrorMessage = "Số chỗ trong khu vực không được để trống.")]
        [Range(0, 40, ErrorMessage = "Số chỗ trong khu vực phải lớn hơn 0.")]
        public int Size { get; set; }
    }
}
