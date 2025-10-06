using Azure;
using Azure.Data.Tables;

namespace ABCRetailFunctions.Models;

public class CustomerEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Customer";
    public string RowKey { get; set; } = Guid.NewGuid().ToString();
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}

public class ProductEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Product";
    public string RowKey { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Sku { get; set; } = "";
    public decimal Price { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}

public class OrderEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Order";
    public string RowKey { get; set; } = Guid.NewGuid().ToString();
    public string CustomerId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}




