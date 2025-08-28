using System.Collections.Generic;
namespace ABCRetailStorageApp.Models
{
    public class TablesView
    {
        public List<CustomerEntity> Customers { get; set; } = new();
        public List<ProductEntity> Products { get; set; } = new();
    }
}
