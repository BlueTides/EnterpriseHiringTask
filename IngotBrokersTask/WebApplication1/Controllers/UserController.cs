using Microsoft.AspNetCore.Mvc;
using WebApplication1.DataBaseContext;
using WebApplication1.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using System.Net.Mail;

namespace WebApplication1.Controllers
{
    public class UserController : ControllerBase
    {
        private readonly Context _DatabaseContext;
        private readonly ILogger<UserController> _logger;
        private readonly IConfiguration _configuration;

        public readonly struct UserRoles {
            public const string Super_Admin = "SuperAdmin";
            public const string Admin = "Admin";
            public const string User = "User";
        }

        private enum DataBaseStates
        {
            Error = 0,
            SucessfulSave
        }

        public UserController(
            ILogger<UserController> logger,
            Context databaseContext,
            IConfiguration configuration)
        {

            _logger = logger;
            _DatabaseContext = databaseContext;
            _configuration = configuration;
        }

        #region POST Requests
        [Route("api/auth/register")]
        [HttpPost]
        public ActionResult Register(string email,string password,string userName,string role)
        {
            try
            {
                List<string> usersRoles = new List<string>{ 
                    UserRoles.Super_Admin.ToLower(), 
                    UserRoles.Admin.ToLower() , 
                    UserRoles.User.ToLower()
                };

                var savingDBState = DataBaseStates.Error.GetHashCode();

                if (!usersRoles.Contains(role.ToLower())) 
                    return BadRequest("Role name is not available");


                try
                {
                    MailAddress m = new MailAddress(email);
                }
                catch (FormatException) {
                    return BadRequest("Bad email format");
                }
                catch (Exception)
                {
                    return BadRequest("Bad email format");
                }

                User? newUser = new User
                {
                    email = email,
                    password = password,
                    userName = userName,
                    role = role
                };

                using (var ctx = _DatabaseContext)
                {
                    ctx.Users.Add(newUser);
                    savingDBState = ctx.SaveChanges();
                }

                if (savingDBState == DataBaseStates.SucessfulSave.GetHashCode())
                    return Ok("Created user sucessfully");
                else
                    return UnprocessableEntity();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "\n----------------------------------------\n\n => ex Error: " + ex);
            }

            return NotFound();
        }

        [Route("api/auth/login")]
        [HttpPost]
        public ActionResult Login(string email, string password)
        {

            try
            {
                bool isMatch = false;
                User? user = null;

                using (var ctx = _DatabaseContext)
                {
                    isMatch = ctx.Users.Any(o => o.email == email && o.password == password);
                    user = ctx.Users.FirstOrDefault(o => o.email == email && o.password == password);
                }

                if (isMatch)
                {
                    string jwtToken = string.Empty;
                    JwtHeader header = new JwtHeader(
                        new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? string.Empty)), SecurityAlgorithms.HmacSha256));

                    JwtPayload payload = new JwtPayload
                {
                    { JwtRegisteredClaimNames.Sub, user?.user_id },
                    { JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                    { JwtRegisteredClaimNames.Iss, _configuration["Jwt:Issuer"] },
                    { JwtRegisteredClaimNames.Aud, _configuration["Jwt:Audience"] },
                    { JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds() },
                    { "roles", user?.role },
                    { "user_id", user?.user_id },
                };

                    JwtSecurityToken token = new JwtSecurityToken(header, payload);
                    JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                    jwtToken = tokenHandler.WriteToken(token);

                    return Ok(jwtToken);
                }

                return NotFound("Wrong username or password");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "\n----------------------------------------\n\n => ex Error: " + ex);

            }

            return NotFound();
        }
        #endregion

        #region GET Requests
        [Route("api/users/profile")]
        [HttpGet]
        [Authorize]
        public string Profile(string email)
        {
            try {

                bool isMatch = false;
                User? user = new User();

                using (var ctx = _DatabaseContext)
                {
                    isMatch = ctx.Users.Any(o => o.email == email);
                    user = ctx?.Users?.FirstOrDefault(o => o.email == email);
                }

                if (isMatch)
                    return System.Text.Json.JsonSerializer.Serialize(user);

                return string.Empty;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "\n----------------------------------------\n\n => ex Error: " + ex);
            }
            return string.Empty;
        }

        [Route("api/users/all")]
        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public string All()
        {

            try {
                List<User>? AllUsers = new List<User>();

                using (var ctx = _DatabaseContext)
                    AllUsers = ctx?.Users?.ToList();

                return System.Text.Json.JsonSerializer.Serialize(AllUsers);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "\n----------------------------------------\n\n => ex Error: " + ex);
            }

            return string.Empty;
        }

        [Route("api/users")]
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public string Users()
        {
            try {
                List<User>? AdminsUsers = new List<User>();

                using (var ctx = _DatabaseContext)
                    AdminsUsers = ctx?.Users?.Where(o => o.role.ToLower() == UserRoles.Super_Admin.ToLower() ||
                                                                      o.role.ToLower() == UserRoles.Admin.ToLower())?.ToList();
                return System.Text.Json.JsonSerializer.Serialize(AdminsUsers);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "\n----------------------------------------\n\n => ex Error: " + ex);
            }
            return string.Empty;
        }
        #endregion
    }
}
