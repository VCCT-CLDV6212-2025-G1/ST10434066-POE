using System.Data.SqlClient;
using System.Security.Claims;
using ABCRetailStorageApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Data.SqlClient;

namespace ABCRetailStorageApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _config;

        public AccountController(IConfiguration config)
        {
            _config = config;
        }
        // Getter for Login
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }
        //Post method for Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                model.ErrorMessage = "Enter your email and password.";
                return View(model);
            }
            // Getting string from appsettings.json
            var connString = _config.GetConnectionString("SqlConnection");
            if (string.IsNullOrWhiteSpace(connString))
            {
                model.ErrorMessage = "Database not Configured";
                return View(model);
            }

            string? role = null;
            string? fullName = null;
            // paramaters for sql connection
            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(
                "SELECT FullName, Role FROM Users WHERE Email = @Email AND Password = @Password", conn))
            {
                cmd.Parameters.AddWithValue("@Email", model.Email);
                cmd.Parameters.AddWithValue("@Password", model.Password);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    fullName = reader["FullName"] as string;
                    role = reader["Role"] as string;
                }
            }

            if (role == null)
            { // invalid login
                model.ErrorMessage = "Email or Password is incorrect.";
                return View(model);
            }
            // claims 
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, fullName ?? model.Email),
                new Claim(ClaimTypes.Email, model.Email),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (role == "Admin")
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return RedirectToAction("Index", "Products");
            }
        }
        // logout method
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}