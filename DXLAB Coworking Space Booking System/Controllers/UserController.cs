using DxLabCoworkingSpace;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;



namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _config;
        private IUserService _userService;
        //private readonly IHttpClientFactory _httpClientFactory;

        public UserController(IConfiguration config, IUserService userService/*, IHttpClientFactory httpClientFactory*/)
        {
            _config = config;
            _userService = userService;
            //_httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        public IActionResult VerifyAccount([FromBody] User userinfo)
        {
            if(!ModelState.IsValid)
                return BadRequest(ModelState);
            var user = _userService.Get(x => x.Email == userinfo.Email);
            
           
                if (user == null)
                {
                    user = new User
                    {
                        Email = userinfo.Email,
                        WalletAddress = userinfo.WalletAddress,
                        RoleId = 3,
                        FullName = userinfo.FullName
                        ,
                        Status = true
                    };

                    _userService.Add(user);
                }


                var token = GenerateJwtToken(user);
                return Ok(token);

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
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        //[HttpPost]
        //[Route("api/auth")]
        //public async Task<IActionResult> Auth([FromBody] AuthPayload payload)
        //{
        //    try
        //    {
        //        var token = payload.Payload;
        //        var validationSettings = new GoogleJsonWebSignature.ValidationSettings
        //        {
        //            Audience = new[] { "399e21caec21e6a96434eb49a0da5d13" } 
        //        };
        //        var payloadData = await GoogleJsonWebSignature.ValidateAsync(token, validationSettings);
        //        var email = payloadData.Email;
        //        if (email.EndsWith("@fpt.edu.vn"))
        //        {

        //            var user = new User()
        //            {
        //                Email = email
        //            };
        //            return Ok(GenerateJwtToken(user));
        //        }
        //        else
        //        {
                    
        //            return Unauthorized();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { error = ex.Message });
        //    }
        //}

    }

   

    public class AuthPayload
    {
        public string Payload { get; set; }
    }
}
