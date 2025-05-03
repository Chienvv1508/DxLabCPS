using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class AccountDTO
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "RoleName không được để trống.")]
        public string RoleName { get; set; } = null!;

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [RegularExpression(@"^[^@]+@[^@]+\.[^@]+$", ErrorMessage = "Email phải có định dạng hợp lệ (chứa @ và .).")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "FullName không được để trống.")]
        [MinLength(5, ErrorMessage = "FullName phải có ít nhất 5 ký tự.")]
        public string FullName { get; set; } = null!;
        public bool Status { get; set; }

    }
}
