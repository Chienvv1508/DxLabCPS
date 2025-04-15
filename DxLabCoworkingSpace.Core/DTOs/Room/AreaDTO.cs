using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class AreaDTO
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int AreaId { get; set; }
        [Required(ErrorMessage = "Bạn chưa nhập loại khu vực khi tạo phòng")]
        public int AreaTypeId { get; set; }
        public string? AreaTypeName { get; set; }
        [Required(ErrorMessage = "Bạn chưa nhập tên khu vực")]
        public string AreaName { get; set; }

        public bool IsAvail { get; set; }
        public string? AreaDescription { get; set; }

        public List<String>? Images { get; set; }
    }
}
