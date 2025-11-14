using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ABCRetailStorageApp.Models;
using System.Security.Claims;

namespace ABCRetailStorageApp.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IConfiguration _config;

        public OrdersController(IConfiguration config)
        {
            _config = config;
        }

        private string Conn => _config.GetConnectionString("SqlConnection");

        
        public IActionResult MyOrders()
        {
            if (!User.Identity?.IsAuthenticated ?? false)
                return RedirectToAction("Login", "Account");

        
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                TempData["Msg"] = "Could not identify user.";
                return RedirectToAction("Index", "Home");
            }

            int userId;
            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                using var cmd = new SqlCommand("SELECT Id FROM Users WHERE Email = @Email", conn);
                cmd.Parameters.AddWithValue("@Email", email);
                var result = cmd.ExecuteScalar();
                if (result == null)
                {
                    TempData["Msg"] = "User not found in database.";
                    return RedirectToAction("Index", "Home");
                }
                userId = Convert.ToInt32(result);
            }

            var orders = new List<Order>();

            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "SELECT Id, UserId, OrderDate, Status, TotalAmount FROM Orders WHERE UserId=@uid ORDER BY OrderDate DESC",
                    conn);

                cmd.Parameters.AddWithValue("@uid", userId);

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    orders.Add(new Order
                    {
                        Id = r.GetInt32(0),
                        UserId = r.GetInt32(1),
                        OrderDate = r.GetDateTime(2),
                        Status = r.GetString(3),
                        TotalAmount = r.GetDecimal(4)
                    });
                }
            }

            return View(orders);
        }

   
        public IActionResult AdminOrders()
        {
            var orders = new List<Order>();

            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "SELECT Id, UserId, OrderDate, Status, TotalAmount FROM Orders ORDER BY OrderDate DESC",
                    conn);

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    orders.Add(new Order
                    {
                        Id = r.GetInt32(0),
                        UserId = r.GetInt32(1),
                        OrderDate = r.GetDateTime(2),
                        Status = r.GetString(3),
                        TotalAmount = r.GetDecimal(4)
                    });
                }
            }

            return View(orders);
        }

        [HttpPost]
        public IActionResult Process(int id)
        {
            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "UPDATE Orders SET Status='PROCESSED' WHERE Id=@id",
                    conn);

                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }

            TempData["Msg"] = "Order processed.";
            return RedirectToAction("AdminOrders");
        }
    }
}