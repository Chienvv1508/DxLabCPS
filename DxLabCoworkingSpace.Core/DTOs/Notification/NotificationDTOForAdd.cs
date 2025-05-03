using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class NotificationDTOForAdd
    {

        public int NotificationId { get; set; }
        [Required(ErrorMessage = "Message không được để trống.")]
        [StringLength(500, ErrorMessage = "Message không được vượt quá 500 ký tự.")]
        public string Message { get; set; } = null!;
        public int UserId { get; set; }
    }
}
