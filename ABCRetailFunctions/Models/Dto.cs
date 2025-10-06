using ABCRetailFunctions.Models;

namespace ABCRetailFunctions.Models;

public record AddCustomerDto(string FirstName, string LastName, string Email);
public record AddProductDto(string Name, decimal Price);
public record AddOrderDto(string CustomerId, string ProductId, string? Status);


