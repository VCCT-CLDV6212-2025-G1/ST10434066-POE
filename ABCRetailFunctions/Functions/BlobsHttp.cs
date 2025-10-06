using System.Net;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;

namespace ABCRetailFunctions.Functions
{
    public class BlobsHttp
    {
        private readonly string _conn;
        private readonly string _container = "product-images";

        public BlobsHttp(IConfiguration config)
        {
            _conn = config["StorageConnection"];
        }

        private BlobContainerClient Container()
        {
            var c = new BlobContainerClient(_conn, _container);
            c.CreateIfNotExists();
            return c;
        }

        // GET Blobs
        [Function("BlobsList")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blobs")] HttpRequestData req)
        {
            var container = Container();
            var items = new List<object>();
            await foreach (var b in container.GetBlobsAsync())
            {
                var blob = container.GetBlobClient(b.Name);
                Uri url = blob.Uri;

                // generate SAS
               if (blob.CanGenerateSasUri)
                {
                    var sas = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(2))
                    {
                        BlobContainerName = _container,
                        BlobName = b.Name,
                        Resource = "b"
                    };
                    url = blob.GenerateSasUri(sas);
                }

                items.Add(new { name = b.Name, url = url.ToString() });
            }

            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteAsJsonAsync(items);
            return res;
        }

        // POST Blobs
        [Function("BlobsUpload")]
        public async Task<HttpResponseData> Post(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "blobs/upload")] HttpRequestData req)
        {
            if (!req.Headers.TryGetValues("Content-Type", out var ct) || !ct.First().Contains("multipart/form-data"))
                return req.CreateResponse(HttpStatusCode.BadRequest);

            // manually parse multipart form data
            var boundary = GetBoundaryFromContentType(ct.First());
            if (string.IsNullOrEmpty(boundary))
                return req.CreateResponse(HttpStatusCode.BadRequest);

            var formSections = await ParseMultipartFormAsync(req.Body, boundary);
            var fileSection = formSections.FirstOrDefault(s => s.IsFile);
            if (fileSection == null || fileSection.FileContent == null || fileSection.FileContent.Length == 0)
                return req.CreateResponse(HttpStatusCode.BadRequest);

            var container = Container();
            var blob = container.GetBlobClient(fileSection.FileName);

            fileSection.FileContent.Position = 0;
            await blob.UploadAsync(fileSection.FileContent, overwrite: true);

            return req.CreateResponse(HttpStatusCode.OK);
        }

        private static string? GetBoundaryFromContentType(string contentType)
        {
            var elements = contentType.Split(';');
            foreach (var element in elements)
            {
                var trimmed = element.Trim();
                if (trimmed.StartsWith("boundary=", StringComparison.OrdinalIgnoreCase))
                    return trimmed.Substring("boundary=".Length);
            }
            return null;
        }

        private class FormSection
        {
            public bool IsFile { get; set; }
            public string? FileName { get; set; }
            public MemoryStream? FileContent { get; set; }
        }

        private static async Task<List<FormSection>> ParseMultipartFormAsync(Stream body, string boundary)
        {
            var sections = new List<FormSection>();
            var reader = new StreamReader(body);
            var content = await reader.ReadToEndAsync();
            var delimiter = $"--{boundary}";
            var parts = content.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (part == "--" || string.IsNullOrWhiteSpace(part)) continue;
                var headerEnd = part.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                if (headerEnd < 0) continue;
                var headers = part.Substring(0, headerEnd);
                var bodyStart = headerEnd + 4;
                var bodyContent = part.Substring(bodyStart).TrimEnd('\r', '\n', '-');

                var isFile = headers.Contains("filename=", StringComparison.OrdinalIgnoreCase);
                if (isFile)
                {
                    var fileNameStart = headers.IndexOf("filename=\"", StringComparison.OrdinalIgnoreCase);
                    var fileNameEnd = headers.IndexOf("\"", fileNameStart + 10, StringComparison.OrdinalIgnoreCase);
                    var fileName = headers.Substring(fileNameStart + 10, fileNameEnd - (fileNameStart + 10));

                    var fileBytes = System.Text.Encoding.UTF8.GetBytes(bodyContent);
                    sections.Add(new FormSection
                    {
                        IsFile = true,
                        FileName = fileName,
                        FileContent = new MemoryStream(fileBytes)
                    });
                }
            }
            return sections;
        }
    }
}

