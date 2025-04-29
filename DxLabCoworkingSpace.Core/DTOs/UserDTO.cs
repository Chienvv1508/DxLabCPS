using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class UserDTO
    {
        public int UserId { get; set; }
        [DefaultValue("")]
        public string Email { get; set; } = null!;

        public string FullName { get; set; } = null!;
        [DefaultValue("")]
        public string? WalletAddress { get; set; }

        public bool Status { get; set; }
        public int? RoleId { get; set; }
    }
}
