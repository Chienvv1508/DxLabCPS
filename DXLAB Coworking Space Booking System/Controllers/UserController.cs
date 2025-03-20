using DxLabCoworkingSpace;
using DxLabCoworkingSpace.Service.Sevices;
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

        [HttpPost("createuser")]
        public async Task<IActionResult> VerifyAccount([FromBody] UserDTO userinfo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<object>(400, "Dữ liệu không hợp lệ!", ModelState));
            }

            try
            {
                var user = await _userService.Get(x => x.Email == userinfo.Email);

                if (user == null)
                {
                    user = new User
                    {
                        Email = userinfo.Email,
                        WalletAddress = userinfo.WalletAddress,
                        RoleId = userinfo.RoleId,
                        FullName = userinfo.FullName,
                        Status = userinfo.Status
                    };

                    await _userService.Add(user);
                }

                var userDto = new UserDTO
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    WalletAddress = user.WalletAddress,
                    RoleId = user.RoleId,
                    FullName = user.FullName,
                    Status = user.Status
                };

                var token = GenerateJwtToken(user);
                var responseData = new { Token = token, User = userDto };
                return Ok(new ResponseDTO<object>(200, "Người dùng đã được tạo hoặc xác thực thành công!", responseData));
                return Ok(new ResponseDTO<object>(200, "Người dùng đã được tạo hoặc xác thực thành công!", responseData));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi xử lý người dùng: {ex.Message}", null));
            }
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email)
          
        };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30), 
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

