namespace ABCRetailStorageApp.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }         
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = "PENDING";
        public decimal TotalAmount { get; set; }

        public List<OrderItem> Items { get; set; } = new();
    }
}