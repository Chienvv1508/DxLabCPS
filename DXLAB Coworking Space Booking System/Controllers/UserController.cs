using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _config;
        private IUserService _userService;

        public UserController(IConfiguration config, IUserService userService)
        {
            _config = config;
            _userService = userService;
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
                new Claim("UserId", user.UserId.ToString()), // Thêm UserId
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(ClaimTypes.Role, user.Role.RoleName), // Thêm RoleName
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        //Verify User
        [HttpPost("verifyuser")]
        public async Task<IActionResult> VerifyAccount([FromBody] UserDTO userinfo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<object>(400, "Dữ liệu không hợp lệ!", ModelState));
            }

            try
            {
                // Sử dụng GetWithInclude thay vì Get
                var user = await _userService.GetWithInclude(x => x.Email == userinfo.Email, u => u.Role);

                // Trường hợp user đã tồn tại
                if (user != null)
                {
                    if (user.Role == null)
                    {
                        return StatusCode(500, new ResponseDTO<object>(500, "Lỗi: Người dùng chưa được gán Role!", null));
                    }

                    var token = GenerateJwtToken(user);
                    user.AccessToken = token;
                    user.WalletAddress = userinfo.WalletAddress;
                    await _userService.Update(user);

                    var userDto = new UserDTO
                    {
                        UserId = user.UserId,
                        Email = user.Email,
                        WalletAddress = user.WalletAddress,
                        RoleId = user.RoleId,
                        FullName = user.FullName,
                        Status = user.Status
                    };

                    var responseData = new { Token = token, User = userDto };
                    return Ok(new ResponseDTO<object>(200, "Người dùng đã được xác thực thành công!", responseData));
                }

                // Trường hợp user chưa tồn tại
                if (userinfo.Email.EndsWith("@fpt.edu.vn", StringComparison.OrdinalIgnoreCase))
                {
                    var newUser = new User
                    {
                        Email = userinfo.Email,
                        FullName = userinfo.FullName,
                        RoleId = 3, // RoleId = 3 cho Student
                        Status = true,
                        WalletAddress = userinfo.WalletAddress
                    };

                    await _userService.Add(newUser);

                    // Sử dụng GetWithInclude để load Role cho user mới
                    var savedUser = await _userService.GetWithInclude(x => x.UserId == newUser.UserId, u => u.Role);
                    if (savedUser == null || savedUser.Role == null)
                    {
                        return StatusCode(500, new ResponseDTO<object>(500, "Lỗi: Không thể load Role cho user mới!", null));
                    }

                    var token = GenerateJwtToken(savedUser);
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

                    var responseData = new { Token = token, User = userDto };
                    return Ok(new ResponseDTO<object>(201, "Người dùng mới (Student) đã được tạo và xác thực thành công!", responseData));
                }
                else
                {
                    // Email thường không tồn tại trong hệ thống
                    return Unauthorized(new ResponseDTO<object>(401, "Email không tồn tại trong hệ thống!", null));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi xử lý người dùng: {ex.Message}", null));
            }
        }
    }
}

