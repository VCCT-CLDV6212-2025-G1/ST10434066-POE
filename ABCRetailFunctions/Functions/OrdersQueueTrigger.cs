using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using ABCRetailFunctions.Models;

namespace ABCRetailFunctions.Functions
{
    public class OrdersQueueTrigger
    {
        private readonly TableServiceClient _svc;
        private const string TableName = "Orders";

        public OrdersQueueTrigger(IConfiguration config)
        {
            _svc = new TableServiceClient(config["StorageConnection"]);
        }

        
        [Function("OrdersQueueTrigger")]
        public async Task Run([QueueTrigger("orders", Connection = "StorageConnection")] string message)
        {
            var parts = (message ?? "").Split('|');
            var order = new OrderEntity
            {
                CustomerId = parts.ElementAtOrDefault(0) ?? "",
                ProductId = parts.ElementAtOrDefault(1) ?? "",
                Status = string.IsNullOrWhiteSpace(parts.ElementAtOrDefault(2)) ? "Pending" : parts[2]
            };

            var table = _svc.GetTableClient(TableName);
            await table.CreateIfNotExistsAsync();
            await table.AddEntityAsync(order);
        }
    }
}


