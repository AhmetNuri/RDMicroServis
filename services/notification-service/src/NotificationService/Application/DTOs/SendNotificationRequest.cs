using System.ComponentModel.DataAnnotations;

namespace NotificationService.Application.DTOs;

public class SendNotificationRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public Dictionary<string, object> Metadata { get; set; } = new();
}
