using System.Net;
using System.Text.Json;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using ABCRetailFunctions.Models;

namespace ABCRetailFunctions.Functions
{
    public class ProductsHttp
    {
        private readonly TableServiceClient _svc;
        private const string TableName = "Products";

        public ProductsHttp(IConfiguration config)
        {
            _svc = new TableServiceClient(config["StorageConnection"]);
        }

        // GET Products
        [Function("ProductsGet")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequestData req)
        {
            var table = _svc.GetTableClient(TableName);
            await table.CreateIfNotExistsAsync();

            var all = table.Query<ProductEntity>(p => p.PartitionKey == "Product").ToList();

           
            var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query).Get("q");
            var rows = string.IsNullOrWhiteSpace(q)
                ? all
                : all.Where(x => x.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) == true).ToList();

            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteAsJsonAsync(rows);
            return res;
        }

        // POST Products
       
        [Function("ProductsAdd")]
        public async Task<HttpResponseData> Post(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products")] HttpRequestData req)
        {
            var dto = await JsonSerializer.DeserializeAsync<AddProductDto>(
                req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dto is null || string.IsNullOrWhiteSpace(dto.Name))
                return req.CreateResponse(HttpStatusCode.BadRequest);

            var table = _svc.GetTableClient(TableName);
            await table.CreateIfNotExistsAsync();

            await table.AddEntityAsync(new ProductEntity
            {
                Name = dto.Name,
                Sku = $"SKU-{Guid.NewGuid():N}".Substring(0, 8),
                Price = dto.Price
            });

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}