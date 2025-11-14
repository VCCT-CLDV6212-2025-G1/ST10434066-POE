using ABCRetailStorageApp.Models;
using ABCRetailStorageApp.Services;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ABCRetailStorageApp.Controllers
{
    public class ProductsController : Controller
    {
        private readonly TableStorageService _tables;
        private readonly string _storageConnection;
        private readonly string _blobContainer;

        public ProductsController(TableStorageService tables, IOptions<AzureStorageOptions> storageOptions)
        {
            _tables = tables;

            var opt = storageOptions.Value;
            _storageConnection = opt.ConnectionString;
            _blobContainer = opt.BlobContainer;  
        }

        private Uri BuildImageSas(string blobName)
        {
            var container = new BlobContainerClient(_storageConnection, _blobContainer);
            container.CreateIfNotExists();

            var blob = container.GetBlobClient(blobName);

           
            if (blob.CanGenerateSasUri)
            {
                var sas = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1))
                {
                    BlobContainerName = _blobContainer,
                    BlobName = blobName,
                    Resource = "b"
                };

                return blob.GenerateSasUri(sas);
            }

            return blob.Uri;
        }

        public IActionResult Index()
        {
          
            var vm = _tables.ListAll();
            var products = vm.Products ?? new List<ProductEntity>();

          
            foreach (var p in products)
            {
                try
                {
                    var blobName = $"{p.Sku}.jpg";      
                    var uri = BuildImageSas(blobName);
                    p.ImageUrl = uri.ToString();
                }
                catch
                {
                    
                    p.ImageUrl = null;
                }
            }

            return View(products);
        }
    }
}