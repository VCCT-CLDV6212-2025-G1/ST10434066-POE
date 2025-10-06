using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ABCRetailStorageApp.Models;

namespace ABCRetailStorageApp.Controllers
{
    public class TablesController : Controller
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _cfg;

        public TablesController(IHttpClientFactory http, IConfiguration cfg)
        {
            _http = http;
            _cfg = cfg;
        }

        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Seed()
        {
            var client = _http.CreateClient();

            // Seed 5 customers
            var customersUrl = _cfg["FunctionUrls:Customers"] ?? throw new InvalidOperationException("FunctionUrls:Customers missing");
            for (int i = 1; i <= 5; i++)
                await client.PostAsJsonAsync(customersUrl, new { firstName = $"Name{i}", lastName = $"Surname{i}", email = $"user{i}@test.com" });

            // Seed 5 products
            var productsUrl = _cfg["FunctionUrls:Products"] ?? throw new InvalidOperationException("FunctionUrls:Products missing");
            for (int i = 1; i <= 5; i++)
                await client.PostAsJsonAsync(productsUrl, new { name = $"Product {i}", price = 99 + i });

            return RedirectToAction(nameof(List));
        }

        public async Task<IActionResult> List(string? q = null)
        {
            var client = _http.CreateClient();

            var customersUrl = _cfg["FunctionUrls:Customers"] ?? throw new InvalidOperationException("FunctionUrls:Customers missing");
            var productsBase = _cfg["FunctionUrls:Products"] ?? throw new InvalidOperationException("FunctionUrls:Products missing");
            var productsUrl = string.IsNullOrWhiteSpace(q) ? productsBase : $"{productsBase}?q={Uri.EscapeDataString(q)}";

            var vm = new TablesView
            {
                Customers = await client.GetFromJsonAsync<List<CustomerEntity>>(customersUrl) ?? new(),
                Products = await client.GetFromJsonAsync<List<ProductEntity>>(productsUrl) ?? new()
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> AddCustomer(string name, string email)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                return RedirectToAction(nameof(List));

            var first = name;
            var last = "";

            var client = _http.CreateClient();
            var customersUrl = _cfg["FunctionUrls:Customers"]!;
            await client.PostAsJsonAsync(customersUrl, new { firstName = first, lastName = last, email });
            TempData["Msg"] = "Customer added";
            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(string name, decimal price)
        {
            if (string.IsNullOrWhiteSpace(name)) return RedirectToAction(nameof(List));
            var client = _http.CreateClient();
            var productsUrl = _cfg["FunctionUrls:Products"]!;
            await client.PostAsJsonAsync(productsUrl, new { name, price });
            TempData["Msg"] = "Product added";
            return RedirectToAction(nameof(List));
        }
    }
}