using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class User
    {
        public int UserId { get; set; }
        public string Email {  get; set; }
        public string WalletAddress { get; set; }

        public int RoleId { get; set; }
    }
}
