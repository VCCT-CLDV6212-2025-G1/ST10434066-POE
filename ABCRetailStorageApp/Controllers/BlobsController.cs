using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ABCRetailStorageApp.Models;

namespace ABCRetailStorageApp.Controllers
{
    public class BlobsController : Controller
    {
        private readonly string _conn;
        private readonly string _container;

        public BlobsController(IOptions<AzureStorageOptions> opt)
        {
            _conn = opt.Value.ConnectionString;
            _container = opt.Value.BlobContainer;
        }

        private BlobContainerClient GetContainer()
        {
            
            var client = new BlobContainerClient(_conn, _container);
            client.CreateIfNotExists();
            return client;
        }

        private Uri GetReadSasUri(BlobClient blob, int minutes = 120)
        {
        
            if (blob.CanGenerateSasUri)
            {
                var sas = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(minutes))
                {
                    BlobContainerName = _container,
                    BlobName = blob.Name,
                    Resource = "b"
                };
                return blob.GenerateSasUri(sas);
            }
          
            return blob.Uri;
        }

        public async Task<IActionResult> Index()
        {
            var container = GetContainer();
            var items = new List<BlobItemModel>();

            await foreach (var b in container.GetBlobsAsync())
            {
                var blob = container.GetBlobClient(b.Name);
                var sas = GetReadSasUri(blob);
                items.Add(new BlobItemModel { Name = b.Name, Url = sas.ToString() });
            }

            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0) return RedirectToAction(nameof(Index));

            var container = GetContainer();
            var blob = container.GetBlobClient(file.FileName);
            await blob.UploadAsync(file.OpenReadStream(), overwrite: true);

            return RedirectToAction(nameof(Index));
        }
    }
}