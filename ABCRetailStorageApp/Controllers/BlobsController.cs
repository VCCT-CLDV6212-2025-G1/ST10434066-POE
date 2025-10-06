using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace ABCRetailStorageApp.Controllers
{
    public class BlobsController : Controller
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _cfg;

        public BlobsController(IHttpClientFactory http, IConfiguration cfg)
        {
            _http = http;
            _cfg = cfg;
        }

        public async Task<IActionResult> Index()
        {
            var listUrl = _cfg["FunctionUrls:BlobsList"] ?? throw new InvalidOperationException("FunctionUrls:BlobsList missing");
            var items = await _http.CreateClient().GetFromJsonAsync<List<dynamic>>(listUrl) ?? new();
            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0) return RedirectToAction(nameof(Index));

            var uploadUrl = _cfg["FunctionUrls:BlobsUpload"] ?? throw new InvalidOperationException("FunctionUrls:BlobsUpload missing");

            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            content.Add(new StreamContent(stream), "file", file.FileName);

            await _http.CreateClient().PostAsync(uploadUrl, content);
            return RedirectToAction(nameof(Index));
        }
    }
}
