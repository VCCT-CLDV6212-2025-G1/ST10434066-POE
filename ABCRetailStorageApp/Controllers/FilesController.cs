using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace ABCRetailStorageApp.Controllers
{
    public class FilesController : Controller
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _cfg;

        public FilesController(IHttpClientFactory http, IConfiguration cfg)
        {
            _http = http;
            _cfg = cfg;
        }

        public async Task<IActionResult> Index()
        {
            var listUrl = _cfg["FunctionUrls:FilesList"] ?? throw new InvalidOperationException("FunctionUrls:FilesList missing");
            var items = await _http.CreateClient().GetFromJsonAsync<List<string>>(listUrl) ?? new();
            ViewBag.Count = items.Count;
            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0) return RedirectToAction(nameof(Index));

            var uploadUrl = _cfg["FunctionUrls:FilesUpload"] ?? throw new InvalidOperationException("FunctionUrls:FilesUpload missing");

            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            content.Add(new StreamContent(stream), "file", file.FileName);

            await _http.CreateClient().PostAsync(uploadUrl, content);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Seed()
        {
            var uploadUrl = _cfg["FunctionUrls:FilesUpload"] ?? throw new InvalidOperationException("FunctionUrls:FilesUpload missing");

            for (int i = 1; i <= 5; i++)
            {
                var name = $"Contract{i}.txt";
                var bytes = System.Text.Encoding.UTF8.GetBytes($"Dummy Contract {i} - {DateTime.UtcNow:o}");
                using var content = new MultipartFormDataContent();
                content.Add(new ByteArrayContent(bytes), "file", name);
                await _http.CreateClient().PostAsync(uploadUrl, content);
            }
            return RedirectToAction(nameof(Index));
        }

      
    }
}
