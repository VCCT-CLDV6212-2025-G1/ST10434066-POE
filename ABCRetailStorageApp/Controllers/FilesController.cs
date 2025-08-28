using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ABCRetailStorageApp.Controllers
{
    public class FilesController : Controller
    {
        private readonly string _conn;
        private readonly string _share;

        public FilesController(IOptions<AzureStorageOptions> opt)
        {
            _conn = opt.Value.ConnectionString;
            _share = opt.Value.FileShare; 
        }

        private ShareClient Share()
        {
            var s = new ShareClient(_conn, _share);
            s.CreateIfNotExists();
            return s;
        }

        public async Task<IActionResult> Index()
        {
            var root = Share().GetRootDirectoryClient();
            var items = new List<string>();
            await foreach (ShareFileItem i in root.GetFilesAndDirectoriesAsync())
                if (!i.IsDirectory) items.Add(i.Name);
            ViewBag.Count = items.Count;
            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0) return RedirectToAction(nameof(Index));
            var root = Share().GetRootDirectoryClient();
            var f = root.GetFileClient(file.FileName);
            await f.CreateAsync(file.Length);
            using var s = file.OpenReadStream();
            await f.UploadRangeAsync(new HttpRange(0, file.Length), s);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Seed()
        {
            var root = Share().GetRootDirectoryClient();
            for (int i = 1; i <= 5; i++)
            {
                var name = $"Contract{i}.txt";
                var fc = root.GetFileClient(name);
                var bytes = System.Text.Encoding.UTF8.GetBytes($"Dummy Contract {i} - {DateTime.UtcNow:o}");

                using var ms = new MemoryStream(bytes);
                await fc.CreateAsync(bytes.Length);
                await fc.UploadRangeAsync(new HttpRange(0, bytes.Length), ms);
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Download(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return RedirectToAction(nameof(Index));
            var root = Share().GetRootDirectoryClient();
            var f = root.GetFileClient(name);
            if (!await f.ExistsAsync()) return RedirectToAction(nameof(Index));
            var dl = await f.DownloadAsync();
            return File(dl.Value.Content, "application/octet-stream", name);
        }
    }
}