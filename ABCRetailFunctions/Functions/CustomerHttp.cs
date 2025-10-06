using System.Net;
using System.Text.Json;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using ABCRetailFunctions.Models;

namespace ABCRetailFunctions.Functions
{
    public class CustomersHttp
    {
        private readonly TableServiceClient _svc;
        private const string TableName = "Customers";

        public CustomersHttp(IConfiguration config)
        {
         
            _svc = new TableServiceClient(config["StorageConnection"]);
        }

        // GET Customers
        [Function("CustomersGet")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers")] HttpRequestData req)
        {
            var table = _svc.GetTableClient(TableName);
            await table.CreateIfNotExistsAsync();

            var rows = table.Query<CustomerEntity>(c => c.PartitionKey == "Customer").ToList();

            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteAsJsonAsync(rows);
            return res;
        }

        // POST Customers

        [Function("CustomersAdd")]
        public async Task<HttpResponseData> Post(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers")] HttpRequestData req)
        {
            var dto = await JsonSerializer.DeserializeAsync<AddCustomerDto>(
                req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dto is null || string.IsNullOrWhiteSpace(dto.Email))
                return req.CreateResponse(HttpStatusCode.BadRequest);

            var table = _svc.GetTableClient(TableName);
            await table.CreateIfNotExistsAsync();

            await table.AddEntityAsync(new CustomerEntity
            {
                FirstName = dto.FirstName ?? "",
                LastName = dto.LastName ?? "",
                Email = dto.Email ?? ""
            });

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
