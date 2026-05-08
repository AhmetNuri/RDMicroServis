using System.ComponentModel.DataAnnotations;

namespace OrderService.Application.DTOs;

public class CreateOrderRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MinLength(1)]
    public List<OrderItemRequest> Items { get; set; } = [];

    public string? Notes { get; set; }
}
