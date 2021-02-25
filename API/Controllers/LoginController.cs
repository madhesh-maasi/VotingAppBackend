using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BAL;
using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class LoginController : ControllerBase
    {
        IUserServices userServices;
        private IConfiguration config;
        public LoginController(IUserServices _userServices, IConfiguration _config)
        {
            userServices = _userServices;
            config = _config;
        }

        [HttpPost]
        [Route("userlogin")]
        public IActionResult UserLogin([FromForm] LoginInfo model)
        {
            IActionResult response = Unauthorized();
            var userDetail = userServices.GetUserInfo(model.UserName, model.Password);

            if (userDetail != null)
            {
                var tokenString = GenerateJSONWebToken(userDetail);
                response = Ok(new { token = tokenString, userName = userDetail.UserName, Email = userDetail.Email, UserId = userDetail.Id, Role = userDetail.Role });
            }
            return response;
        }

        private string GenerateJSONWebToken(LoginInputDTO userInfo)
        {
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                                {
                            new Claim(JwtRegisteredClaimNames.Sub, userInfo.UserName),
                            new Claim(JwtRegisteredClaimNames.Email, userInfo.Email),
                            new Claim("Id",userInfo.Id.ToString()),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            };

                var token = new JwtSecurityToken(config["Jwt:Issuer"],
                  config["Jwt:Issuer"],
                  claims,
                  expires: DateTime.Now.AddMinutes(120),
                  signingCredentials: credentials);

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
        }
    }
}
