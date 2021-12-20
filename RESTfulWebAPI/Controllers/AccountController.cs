using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NSwag.Annotations;
using RESTfulWebAPI.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace RESTfulWebAPI.Controllers
{
    [AllowAnonymous]
    [Route("[controller]")]
    [ApiController]
    [OpenApiTag("Account", Description = "Methods for Authentication and Authorization")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }
        [HttpPost("register")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(IdentityUser), Description = "Successfully User Registration")]
        [SwaggerResponse(HttpStatusCode.BadRequest, null, Description = "User Registration Failed")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {           
            Role role = Role.Student;         
            IdentityUser user = new IdentityUser()
            {
                UserName = model.Username,
                Email = model.Email                
            };
        
            IdentityResult userResult = await _userManager.CreateAsync(user,model.Password);

            if(userResult.Succeeded)
            {
              
                bool roleExcist = await _roleManager.RoleExistsAsync(role.ToString());
               
                if (!roleExcist)
                    await AddRole(role);
               
                var roleResult = await _userManager.AddToRoleAsync(user, role.ToString());

                if (roleResult.Succeeded)
                    return Ok(user);
              
                foreach (var error in roleResult.Errors)
                    ModelState.AddModelError(error.Code, error.Description);
            }
          
            foreach (var error in userResult.Errors)
                ModelState.AddModelError(error.Code, error.Description);

            return BadRequest(ModelState.Values);
        }
        [HttpPost("signIn")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(string), Description = "Logged In Successfully")]
        public async Task<IActionResult> SignIn(SignInViewModel model)
        {          
            var signInResult = await _signInManager.PasswordSignInAsync(model.Username, model.Password,false,false);          
            if (signInResult.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(model.Username);
                var roles = await _userManager.GetRolesAsync(user);

                IdentityOptions identityOptions = new IdentityOptions();

                var claims = new Claim[]
                {
                    new Claim(identityOptions.ClaimsIdentity.UserIdClaimType,user.Id),
                    new Claim(identityOptions.ClaimsIdentity.UserNameClaimType,user.UserName),
                    new Claim(identityOptions.ClaimsIdentity.RoleClaimType,roles[0])
                };

                var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("this-is-my-secret-key"));
                var signingCredentials = new SigningCredentials(signingKey,SecurityAlgorithms.HmacSha256);
                var token = new JwtSecurityToken(signingCredentials: signingCredentials, expires: DateTime.Now.AddHours(3),claims: claims);

                var obj = new
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    UserId = user.Id,
                    user.UserName,
                    Role = roles[0]
                };

                return Ok(obj);
            }                
            return BadRequest(ModelState);
        }
        [HttpPost("signOut")]
        [SwaggerResponse(HttpStatusCode.NoContent, null, Description = "Logged Out Successfully")]
        public new async Task<IActionResult> SignOut()
        {           
            await _signInManager.SignOutAsync();
            return NoContent();
        }      
        private async Task<IdentityResult> AddRole(Role roleName)
        {
            var role = new IdentityRole() {Name = roleName.ToString()};
            return await _roleManager.CreateAsync(role);
        }
    }
}
