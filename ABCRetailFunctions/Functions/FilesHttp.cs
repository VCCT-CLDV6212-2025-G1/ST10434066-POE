using System.Net;
using System.Text;
using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Configuration;

namespace ABCRetailFunctions.Functions
{
    public class FilesHttp
    {
        private readonly string _conn;
        private readonly string _share = "contracts";

        public FilesHttp(IConfiguration config)
        {
            _conn = config["StorageConnection"];
        }

        private ShareClient Share()
        {
            var s = new ShareClient(_conn, _share);
            s.CreateIfNotExists();
            return s;
        }

        // GET Files
        [Function("FilesList")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "files")] HttpRequestData req)
        {
            var root = Share().GetRootDirectoryClient();
            var items = new List<string>();

            await foreach (ShareFileItem i in root.GetFilesAndDirectoriesAsync())
                if (!i.IsDirectory) items.Add(i.Name);

            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteAsJsonAsync(items);
            return res;
        }

        // Add this helper method to parse multipart/form-data manually since HttpRequestData does not have ReadFormAsync.
        // This uses MultipartReader from Microsoft.AspNetCore.WebUtilities and requires the Microsoft.AspNetCore.WebUtilities NuGet package.
        private async Task<(string FileName, long Length, Stream FileStream)?> ParseMultipartFileAsync(HttpRequestData req)
        {
            if (!req.Headers.TryGetValues("Content-Type", out var ct))
                return null;

            var contentType = ct.First();
            var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(contentType).Boundary).Value;
            if (string.IsNullOrEmpty(boundary))
                return null;

            var reader = new MultipartReader(boundary, req.Body);
            MultipartSection section;
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                if (hasContentDispositionHeader && contentDisposition.DispositionType.Equals("form-data") && !string.IsNullOrEmpty(contentDisposition.FileName.Value))
                {
                    var fileName = contentDisposition.FileName.Value;
                    var memStream = new MemoryStream();
                    await section.Body.CopyToAsync(memStream);
                    memStream.Position = 0;
                    return (fileName, memStream.Length, memStream);
                }
            }
            return null;
        }

        // POST Files
        [Function("FilesUpload")]
        public async Task<HttpResponseData> Post(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "files/upload")] HttpRequestData req)
        {
            if (!req.Headers.TryGetValues("Content-Type", out var ct) || !ct.First().Contains("multipart/form-data"))
                return req.CreateResponse(HttpStatusCode.BadRequest);

            var fileInfo = await ParseMultipartFileAsync(req);
            if (fileInfo == null || fileInfo.Value.Length == 0)
                return req.CreateResponse(HttpStatusCode.BadRequest);

            var (fileName, length, fileStream) = fileInfo.Value;

            var root = Share().GetRootDirectoryClient();
            var fc = root.GetFileClient(fileName);

            await fc.CreateAsync(length);
            await fc.UploadRangeAsync(new HttpRange(0, length), fileStream);

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
