using System.ComponentModel.DataAnnotations;

namespace OrderService.Application.DTOs;

public class OrderItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public string ProductName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}
