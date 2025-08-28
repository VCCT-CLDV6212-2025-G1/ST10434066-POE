using ABCRetailStorageApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailStorageApp.Controllers
{
    public class TablesController : Controller
    {
        private readonly TableStorageService _svc;
        public TablesController(TableStorageService svc) => _svc = svc;

        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Seed()
        {
            await _svc.SeedAsync();
            return RedirectToAction(nameof(List));
        }

        public IActionResult List()
        {
            var vm = _svc.ListAll();
            return View(vm);
        }
        [HttpPost]
        public async Task<IActionResult> AddCustomer(string name, string email)
        {
            await _svc.AddCustomerAsync(name, email);
            TempData["Msg"] = "Customer added";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(string name, decimal price)
        {
            await _svc.AddProductAsync(name, price);
            TempData["Msg"] = "Product added";
            return RedirectToAction("Index", "Home");
        }
    }
}
