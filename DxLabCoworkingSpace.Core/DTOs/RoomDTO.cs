using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class RoomDTO
    {
        public RoomDTO()
        {
           
        }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int RoomId { get; set; }
        [Required(ErrorMessage = "Tên phòng là bắt buộc.")]
        [RegularExpression(@"^(AL|BE|DE)\d{3}$", ErrorMessage = "Tên phòng phải có định dạng ALxxx, BExxx, hoặc DExxx.")]
        public string RoomName { get; set; } = null!;
        [StringLength(255, ErrorMessage = "Mô tả phòng không được quá 255 ký tự.")]
        public string? RoomDescription { get; set; }
        [Range(1, 40, ErrorMessage = "Số lượng chỗ phải từ 1 đến 40.")]
        public int Capacity { get; set; }
        public bool IsDeleted { get; set; }

        public List<String>? Images { get; set; }
        
    }
}
