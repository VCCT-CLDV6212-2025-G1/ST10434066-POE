using Azure.Data.Tables;
using ABCRetailStorageApp.Models;
using Microsoft.Extensions.Options;

namespace ABCRetailStorageApp.Services
{
    public class TableStorageService
    {
        private readonly TableServiceClient _svc;
        private readonly string _custTable;
        private readonly string _prodTable;

        public TableStorageService(IOptions<AzureStorageOptions> opt)
        {
            var o = opt.Value;
            _svc = new TableServiceClient(o.ConnectionString);
            _custTable = o.TableNameCustomers;
            _prodTable = o.TableNameProducts;
        }

        private TableClient Get(string name)
        {
            var table = _svc.GetTableClient(name);
            table.CreateIfNotExists();
            return table;
        }

        public async Task SeedAsync()
        {
            var cust = Get(_custTable);
            var prod = Get(_prodTable);

            for (int i = 1; i <= 5; i++)
            {
                await cust.AddEntityAsync(new CustomerEntity
                {
                    FirstName = $"Name{i}",
                    LastName = $"Surname{i}",
                    Email = $"user{i}@test.com"
                });
            }

            for (int i = 1; i <= 5; i++)
            {
                await prod.AddEntityAsync(new ProductEntity
                {
                    Name = $"Product {i}",
                    Sku = $"SKU-{i:000}",
                    Price = 99 + i
                });
            }
        }

        public TablesView ListAll()
        {
            var cust = Get(_custTable);
            var prod = Get(_prodTable);

            var customers = cust.Query<CustomerEntity>(c => c.PartitionKey == "Customer").ToList();
            var products = prod.Query<ProductEntity>(p => p.PartitionKey == "Product").ToList();

            return new TablesView
            {
                Customers = customers,
                Products = products
            };

        }
        public async Task AddCustomerAsync(string name, string email)
        {
            var parts = (name ?? "").Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var first = parts.Length > 0 ? parts[0] : name ?? "";
            var last = parts.Length > 1 ? parts[1] : "";
            var cust = _svc.GetTableClient(_custTable);
            cust.CreateIfNotExists();
            await cust.AddEntityAsync(new CustomerEntity { FirstName = first, LastName = last, Email = email ?? "" });
        }

        public async Task AddProductAsync(string name, decimal price)
        {
            var prod = _svc.GetTableClient(_prodTable);
            prod.CreateIfNotExists();
            await prod.AddEntityAsync(new ProductEntity { Name = name ?? "", Sku = $"SKU-{Guid.NewGuid():N}".Substring(0, 8), Price = price });
        }
    }
}
