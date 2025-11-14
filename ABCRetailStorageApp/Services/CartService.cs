using System.Text.Json;
using ABCRetailStorageApp.Models;

// This is my CartService to manage shopping cart operations using session storage.
// Session storage is used to keep track of cart items for each user session.
// It is set to 30 minutes by default.
namespace ABCRetailStorageApp.Services
{
    public class CartService
    {
        private readonly IHttpContextAccessor _http;

        private const string CART_KEY = "CART_ITEMS";

        public CartService(IHttpContextAccessor http)
        {
            _http = http;
        }

        public List<OrderItem> GetCart()
        {
            var data = _http.HttpContext!.Session.GetString(CART_KEY);
            return data == null
                ? new List<OrderItem>()
                : JsonSerializer.Deserialize<List<OrderItem>>(data)!;
        }

        public void SaveCart(List<OrderItem> items)
        {
            string json = JsonSerializer.Serialize(items);
            _http.HttpContext!.Session.SetString(CART_KEY, json);
        }

        public void AddItem(int productId, string productName, decimal price, int quantity = 1)
        {
            var cart = GetCart();

            var existing = cart.FirstOrDefault(x => x.ProductId == productId && x.ProductName == productName);
            if (existing == null)
            {
                cart.Add(new OrderItem
                {
                    ProductId = productId,
                    ProductName = productName,
                    Price = price,
                    Quantity = quantity
                });
            }
            else
            {
                existing.Quantity += quantity;
            }

            SaveCart(cart);
        }

        public void ClearCart()
        {
            _http.HttpContext!.Session.Remove(CART_KEY);
        }
    }
}