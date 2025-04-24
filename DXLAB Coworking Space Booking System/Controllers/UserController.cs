using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Numerics;
using System.Text;
using System.Collections.Concurrent;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _config;
        private IUserService _userService;
        private ILabBookingJobService _labBookingJobService;
        private static readonly ConcurrentDictionary<string, DateTime> _mintedUsers = new ConcurrentDictionary<string, DateTime>();
        private static readonly ConcurrentDictionary<string, DateTime> _lastApiCall = new ConcurrentDictionary<string, DateTime>();

        public UserController(IConfiguration config, IUserService userService, ILabBookingJobService labBookingJobService)
        {
            _config = config;
            _userService = userService;
            _labBookingJobService = labBookingJobService;
        }

        private string GenerateJwtToken(User user)
        {
            if (user == null || user.Role == null)
            {
                throw new Exception("Lỗi: Người dùng hoặc Role không tồn tại!");
            }

            var keyString = _config["Jwt:Key"];
            var keyBytes = Encoding.UTF8.GetBytes(keyString);
            if (keyBytes.Length < 32)
            {
                throw new Exception("JWT Key phải dài ít nhất 32 byte!");
            }

            var key = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("UserId", user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(ClaimTypes.Role, user.Role.RoleName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(120),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("verifyuser")]
        public async Task<IActionResult> VerifyAccount([FromBody] UserDTO userinfo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<object>(400, "Dữ liệu không hợp lệ!", ModelState));
            }

            try
            {
                DateTime currentTime = DateTime.UtcNow.AddHours(7);

                // Kiểm tra thời gian gọi API gần nhất để tránh spam
                if (_lastApiCall.TryGetValue(userinfo.Email, out var lastCallTime))
                {
                    if ((currentTime - lastCallTime).TotalSeconds < 60) // 60 giây
                    {
                        Console.WriteLine($"API call too frequent for {userinfo.Email}. Last call at {lastCallTime}. Skipping minting.");
                        // Tái sử dụng biến user thay vì khai báo lại
                        var existingUser = await _userService.GetWithInclude(x => x.Email == userinfo.Email, u => u.Role);
                        if (existingUser == null)
                        {
                            return Unauthorized(new ResponseDTO<object>(401, "Email không tồn tại trong hệ thống!", null));
                        }

                        var token = GenerateJwtToken(existingUser);
                        existingUser.AccessToken = token;
                        await _userService.Update(existingUser);

                        var userDto = new UserDTO
                        {
                            UserId = existingUser.UserId,
                            Email = existingUser.Email,
                            WalletAddress = existingUser.WalletAddress ?? "NULL",
                            RoleId = existingUser.RoleId,
                            FullName = existingUser.FullName,
                            Status = existingUser.Status
                        };

                        var responseData = new
                        {
                            Token = token,
                            User = userDto,
                            MintStatus = "Bỏ qua mint token vì API được gọi quá nhanh (< 60 giây). Vui lòng thử lại sau.",
                            TransactionHash = (string)null
                        };
                        return Ok(new ResponseDTO<object>(200, "Người dùng đã được xác thực thành công, nhưng bỏ qua mint token do gọi API quá nhanh!", responseData));
                    }
                }

                // Cập nhật thời gian gọi API
                _lastApiCall[userinfo.Email] = currentTime;

                // Khai báo biến user ở đây và tái sử dụng trong toàn bộ phương thức
                var user = await _userService.GetWithInclude(x => x.Email == userinfo.Email, u => u.Role);
                if (user != null)
                {
                    if (user.Role == null)
                    {
                        return StatusCode(500, new ResponseDTO<object>(500, "Lỗi: Người dùng chưa được gán Role!", null));
                    }
                    Console.WriteLine($"Nguoi dung da ton tai: {user.Email}, RoleId: {user.RoleId}");
                    var token = GenerateJwtToken(user);

                    if ((string.IsNullOrEmpty(user.WalletAddress) || user.WalletAddress == "NULL") && !string.IsNullOrEmpty(userinfo.WalletAddress))
                    {
                        Console.WriteLine($"Cap nhat dia chi vi cho nguoi dung {user.UserId} tu {user.WalletAddress} den {userinfo.WalletAddress}");
                        user.WalletAddress = userinfo.WalletAddress;
                        await _userService.Update(user);
                    }

                    bool mintSuccess = false;
                    string mintStatus = "Không có token nào được tạo (ví không hợp lệ hoặc tạo không thành công)";
                    string txHash = null;
                    if (!string.IsNullOrEmpty(user.WalletAddress) && user.WalletAddress != "NULL" && user.RoleId == 3)
                    {
                        if (_mintedUsers.TryGetValue(user.WalletAddress, out var lastMintedTime))
                        {
                            if ((currentTime - lastMintedTime).TotalHours >= 24)
                            {
                                Console.WriteLine($"Tao token cho {user.WalletAddress}: Lan tao moi nhat tai {lastMintedTime}, du dieu kien bay gio.");
                                (mintSuccess, txHash) = await _labBookingJobService.MintTokenForUser(user.WalletAddress);
                                if (mintSuccess)
                                {
                                    _mintedUsers[user.WalletAddress] = currentTime;
                                    mintStatus = "Tạo 100 tokens thành công!";
                                    Console.WriteLine($"Cap nhat thoi gian tao cho {user.WalletAddress} toi {currentTime}");
                                }
                                else
                                {
                                    mintStatus = txHash != null
                                        ? $"Tạo token chưa xác nhận. Transaction hash: {txHash}. Vui lòng kiểm tra trên Sepolia testnet."
                                        : "Tạo token thất bại!";
                                    Console.WriteLine($"Tao token that bai cho {user.WalletAddress}");
                                }
                            }
                            else
                            {
                                mintStatus = "Token đã được tạo hôm nay!";
                                Console.WriteLine($"Bo qua viec tao cho {user.WalletAddress}: Lan tao moi nhat o {lastMintedTime}, qua som.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Tao lan dau cho {user.WalletAddress}");
                            (mintSuccess, txHash) = await _labBookingJobService.MintTokenForUser(user.WalletAddress);
                            if (mintSuccess)
                            {
                                _mintedUsers[user.WalletAddress] = currentTime;
                                mintStatus = "Tạo 100 tokens thành công!";
                                Console.WriteLine($"Dat thoi gian tao cho {user.WalletAddress} toi {currentTime}");
                            }
                            else
                            {
                                mintStatus = txHash != null
                                    ? $"Tạo token chưa xác nhận. Transaction hash: {txHash}. Vui lòng kiểm tra trên Sepolia testnet."
                                    : "Tạo token thất bại!";
                                Console.WriteLine($"Tao that bai cho {user.WalletAddress}");
                            }
                        }
                    }

                    user.AccessToken = token;
                    await _userService.Update(user);

                    var userDto = new UserDTO
                    {
                        UserId = user.UserId,
                        Email = user.Email,
                        WalletAddress = user.WalletAddress ?? "NULL",
                        RoleId = user.RoleId,
                        FullName = user.FullName,
                        Status = user.Status
                    };

                    var responseData = new
                    {
                        Token = token,
                        User = userDto,
                        MintStatus = mintStatus,
                        TransactionHash = txHash
                    };
                    return Ok(new ResponseDTO<object>(200, "Người dùng đã được xác thực thành công!", responseData));
                }

                if (userinfo.Email.EndsWith("@fpt.edu.vn", StringComparison.OrdinalIgnoreCase))
                {
                    var newUser = new User
                    {
                        Email = userinfo.Email,
                        WalletAddress = userinfo.WalletAddress,
                        FullName = userinfo.FullName,
                        RoleId = 3,
                        Status = true
                    };

                    await _userService.Add(newUser);

                    var savedUser = await _userService.GetWithInclude(x => x.UserId == newUser.UserId, u => u.Role);
                    if (savedUser == null || savedUser.Role == null)
                    {
                        return StatusCode(500, new ResponseDTO<object>(500, "Lỗi: Không thể tải Role cho user mới!", null));
                    }

                    var token = GenerateJwtToken(savedUser);

                    bool mintSuccess = false;
                    string txHash = null;
                    string mintStatus = "Không có token nào được tạo (ví không hợp lệ hoặc tạo không thành công)";
                    if (!string.IsNullOrEmpty(savedUser.WalletAddress) && savedUser.WalletAddress != "NULL" && savedUser.RoleId == 3)
                    {
                        (mintSuccess, txHash) = await _labBookingJobService.MintTokenForUser(savedUser.WalletAddress);
                        mintStatus = mintSuccess
                            ? "Tạo 100 tokens thành công!"
                            : txHash != null
                                ? $"Tạo token chưa xác nhận. Transaction hash: {txHash}. Vui lòng kiểm tra trên Sepolia testnet."
                                : "Tạo token thất bại!";
                    }

                    savedUser.AccessToken = token;
                    await _userService.Update(savedUser);

                    var userDto = new UserDTO
                    {
                        UserId = savedUser.UserId,
                        Email = savedUser.Email,
                        WalletAddress = savedUser.WalletAddress,
                        RoleId = savedUser.RoleId,
                        FullName = savedUser.FullName,
                        Status = savedUser.Status
                    };

                    var responseData = new
                    {
                        Token = token,
                        User = userDto,
                        MintStatus = mintStatus,
                        TransactionHash = txHash
                    };
                    return Ok(new ResponseDTO<object>(201, "Người dùng mới (Sinh viên) đã được tạo, xác thực và cấp token thành công!", responseData));
                }
                else
                {
                    Console.WriteLine($"Email {userinfo.Email} khong có đuoi @fpt.edu.vn");
                    return Unauthorized(new ResponseDTO<object>(401, "Email không tồn tại trong hệ thống!", null));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi xử lý người dùng: {ex.Message}", null));
            }
        }
    }
}