using System.Security.Claims;
using ABCRetailStorageApp.Models;
using ABCRetailStorageApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace ABCRetailStorageApp.Controllers
{
    [Authorize] // user must be logged in to use the cart
    public class CartController : Controller
    {
        private readonly CartService _cartService;
        private readonly IConfiguration _config;

        public CartController(CartService cartService, IConfiguration config)
        {
            _cartService = cartService;
            _config = config;
        }

        // Show cart contents
        public IActionResult Index()
        {
            var items = _cartService.GetCart();
            var total = items.Sum(i => i.LineTotal);

            ViewBag.Total = total;
            return View(items);
        }

        // Add item to cart
        [HttpPost]
        public IActionResult Add(int productId, decimal price, int quantity = 1)
        {
            if (quantity < 1) quantity = 1;

            _cartService.AddItem(productId, "", price, quantity);

            
            return RedirectToAction("Index");
        }

        // Show checkout 
        public IActionResult Checkout()
        {
            var items = _cartService.GetCart();
            if (!items.Any())
            {
                TempData["Msg"] = "Cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Total = items.Sum(i => i.LineTotal);
            return View(items);
        }

        //create the order
        [HttpPost]
        public async Task<IActionResult> Confirm()
        {
            var items = _cartService.GetCart();
            if (!items.Any())
            {
                TempData["Msg"] = "Cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                TempData["Msg"] = "Invalid User.";
                return RedirectToAction(nameof(Index));
            }

            var connString = _config.GetConnectionString("SqlConnection");
            if (string.IsNullOrEmpty(connString))
            {
                TempData["Msg"] = "Database not configured.";
                return RedirectToAction(nameof(Index));
            }

            int userId;

            
            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand("SELECT Id FROM Users WHERE Email = @Email", conn))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                await conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                if (result == null)
                {
                    TempData["Msg"] = "User not Found..";
                    return RedirectToAction(nameof(Index));
                }

                userId = Convert.ToInt32(result);
            }

            var totalAmount = items.Sum(i => i.LineTotal);
            int orderId;

            // Order ID
            using (var conn = new SqlConnection(connString))
            {
                await conn.OpenAsync();

                using (var cmd = new SqlCommand(
                    @"INSERT INTO Orders (UserId, OrderDate, Status, TotalAmount)
                      VALUES (@UserId, SYSUTCDATETIME(), @Status, @TotalAmount);
                      SELECT SCOPE_IDENTITY();", conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Status", "PENDING");
                    cmd.Parameters.AddWithValue("@TotalAmount", totalAmount);

                    var result = await cmd.ExecuteScalarAsync();
                    orderId = Convert.ToInt32(result);
                }

                // Order Items insertion
                foreach (var item in items)
                {
                    using var cmdItem = new SqlCommand(
                        @"INSERT INTO OrderItems (OrderId, ProductId, Quantity, Price)
                          VALUES (@OrderId, @ProductId, @Quantity, @Price);", conn);

                    cmdItem.Parameters.AddWithValue("@OrderId", orderId);
                    cmdItem.Parameters.AddWithValue("@ProductId", item.ProductId);
                    cmdItem.Parameters.AddWithValue("@Quantity", item.Quantity);
                    cmdItem.Parameters.AddWithValue("@Price", item.Price);

                    await cmdItem.ExecuteNonQueryAsync();
                }
            }
            // Clear Cart after order.
            _cartService.ClearCart();

            TempData["Msg"] = $"Order #{orderId} placed successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}