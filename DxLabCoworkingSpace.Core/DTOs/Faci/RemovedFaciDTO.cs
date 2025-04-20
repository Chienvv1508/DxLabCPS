using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class RemovedFaciDTO
    {
        [Required(ErrorMessage ="Bạn bắt buộc nhập khu vực")]
        public int AreaId { get; set; }

        [Required(ErrorMessage ="Bạn bắt buộc nhập thiết bị")]
        public int FacilityId { get; set; }

        [Required(ErrorMessage ="Bạn bắt buộc nhập số lượng")]
        [Range(1, int.MaxValue, ErrorMessage ="Bắt buộc nhập số lượng > 1")]
        public int Quantity { get; set; }
        //[Required(ErrorMessage = "Bạn bắt buộc nhập trạng thái")]
        //[Range(1, 2, ErrorMessage = "Chỉ có trạng thái cũ hoặc hỏng")]
        //public int? Status { get; set; }
    }
}
