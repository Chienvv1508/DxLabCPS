using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class UserDTO
    {
        public int UserId { get; set; }
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        //[RegularExpression(@"^[a-zA-Z0-9._%+-]+@fpt\.edu\.vn$", ErrorMessage = "Email phải thuộc miền @fpt.edu.vn.")]
        public string Email { get; set; } = null!;

        public string FullName { get; set; } = null!;
        public string? WalletAddress { get; set; }

        public bool Status { get; set; }

        public int? RoleId { get; set; }
    }
}
