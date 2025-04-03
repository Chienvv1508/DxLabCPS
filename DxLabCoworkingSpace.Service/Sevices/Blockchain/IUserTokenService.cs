using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace.Service.Sevices.Blockchain
{
    public interface IUserTokenService
    {
        Task<string> RegisterUserAsync(string userAddress, string email, bool isStaff); // Gọi registerUser
        Task<string> GrantTokenAsync(string userAddress, BigInteger amount); // Cấp token
        Task<BigInteger> GetFptBalanceAsync(string userAddress); // Kiểm tra số dư
    }
}
