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
using Nethereum.JsonRpc.Client;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _config;
        private IUserService _userService;
        private ILabBookingJobService _labBookingJobService;
        // Lưu trữ thời gian mint gần nhất trong bộ nhớ
        private static readonly ConcurrentDictionary<string, DateTime> _mintedUsers = new ConcurrentDictionary<string, DateTime>();

        public UserController(IConfiguration config, IUserService userService, ILabBookingJobService labBookingJobService)
        {
            _config = config;
            _userService = userService;
            _labBookingJobService = labBookingJobService;
        }

        // Generate Token
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

        // Verify User
        [HttpPost("verifyuser")]
        public async Task<IActionResult> VerifyUser([FromBody] UserDTO userinfo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<object>(400, "Dữ liệu không hợp lệ!", ModelState));
            }

            try
            {
                DateTime currentTime = DateTime.UtcNow.AddHours(7);
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
                        try
                        {
                            await _userService.Update(user);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error updating wallet address for user {user.UserId}: {ex.Message}");
                            return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi cập nhật WalletAddress: {ex.Message}", null));
                        }
                    }

                    string mintStatus = "Không có token nào được tạo (ví không hợp lệ hoặc user không phải Student)";
                    if (!string.IsNullOrEmpty(user.WalletAddress) && user.WalletAddress != "NULL" && user.RoleId == 3)
                    {
                        bool shouldMint = false;
                        if (_mintedUsers.TryGetValue(user.WalletAddress, out var lastMintedTime))
                        {
                            if ((currentTime - lastMintedTime).TotalHours >= 24)
                            {
                                shouldMint = true;
                                Console.WriteLine($"Tao token cho {user.WalletAddress}: Lan tao moi nhat tai {lastMintedTime}, du dieu kien bay gio.");
                            }
                            else
                            {
                                mintStatus = "Token đã được tạo hôm nay!";
                                Console.WriteLine($"Bo qua viec tao cho {user.WalletAddress}: Lan tao moi nhat o {lastMintedTime}, qua som.");
                            }
                        }
                        else
                        {
                            shouldMint = true;
                            Console.WriteLine($"Tao lan dau cho {user.WalletAddress}");
                        }

                        if (shouldMint)
                        {
                            try
                            {
                                bool mintSuccess = await _labBookingJobService.MintTokenForUser(user.WalletAddress);
                                if (mintSuccess)
                                {
                                    _mintedUsers[user.WalletAddress] = currentTime;
                                    mintStatus = "Tạo 100 tokens thành công!";
                                    Console.WriteLine($"Cap nhat thoi gian tao cho {user.WalletAddress} toi {currentTime}");
                                }
                                else
                                {
                                    mintStatus = "Tạo token thất bại!";
                                    Console.WriteLine($"Tao that bai cho {user.WalletAddress}");
                                }
                            }
                            catch (RpcResponseException rpcEx)
                            {
                                Console.WriteLine($"RPC error while minting for {user.WalletAddress}: {rpcEx.Message}");
                                mintStatus = "Tạo token thất bại do lỗi blockchain!";
                                // Không throw ngoại lệ, chỉ ghi log và tiếp tục
                            }
                        }
                    }

                    //user.AccessToken = token;
                    try
                    {
                        await _userService.Update(user);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating user {user.UserId}: {ex.Message}");
                        return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi cập nhật AccessToken: {ex.Message}", null));
                    }

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
                        MintStatus = mintStatus
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

                    try
                    {
                        await _userService.Add(newUser);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error adding new user: {ex.Message}");
                        return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi thêm người dùng mới: {ex.Message}", null));
                    }

                    var savedUser = await _userService.GetWithInclude(x => x.UserId == newUser.UserId, u => u.Role);
                    if (savedUser == null || savedUser.Role == null)
                    {
                        return StatusCode(500, new ResponseDTO<object>(500, "Lỗi: Không thể tải Role cho user mới!", null));
                    }

                    var token = GenerateJwtToken(savedUser);

                    string mintStatus = "Không có token nào được tạo (ví không hợp lệ)";
                    if (!string.IsNullOrEmpty(savedUser.WalletAddress) && savedUser.WalletAddress != "NULL" && savedUser.RoleId == 3)
                    {
                        try
                        {
                            bool mintSuccess = await _labBookingJobService.MintTokenForUser(savedUser.WalletAddress);
                            if (mintSuccess)
                            {
                                _mintedUsers[savedUser.WalletAddress] = currentTime;
                                mintStatus = "Tạo 100 tokens thành công!";
                                Console.WriteLine($"Dat thoi gian tao cho {savedUser.WalletAddress} toi {currentTime}");
                            }
                            else
                            {
                                mintStatus = "Tạo token thất bại!";
                                Console.WriteLine($"Tao that bai cho {savedUser.WalletAddress}");
                            }
                        }
                        catch (RpcResponseException rpcEx)
                        {
                            Console.WriteLine($"RPC error while minting for {savedUser.WalletAddress}: {rpcEx.Message}");
                            mintStatus = "Tạo token thất bại do lỗi blockchain!";
                            // Không throw ngoại lệ, chỉ ghi log và tiếp tục
                        }
                    }

                    //savedUser.AccessToken = token;
                    try
                    {
                        await _userService.Update(savedUser);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating new user {savedUser.UserId}: {ex.Message}");
                        return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi cập nhật AccessToken cho user mới: {ex.Message}", null));
                    }

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
                        MintStatus = mintStatus
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
                Console.WriteLine($"Error processing user {userinfo.Email}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi xử lý người dùng: {ex.Message}", null));
            }
        }
    }
}