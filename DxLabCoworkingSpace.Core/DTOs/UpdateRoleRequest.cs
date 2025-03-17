using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class UpdateRoleRequest
    {
        [Required(ErrorMessage = "RoleName không được để trống.")]
        public string RoleName { get; set; } = null!;
    }
}
