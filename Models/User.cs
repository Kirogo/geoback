using System.ComponentModel.DataAnnotations;

namespace geoback.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    
    public string Role { get; set; } = "User"; // User, Admin, QS, RM
    
    // Simple permissions list stored as JSON or comma-separated if needed.
    // For now, simpler to just use Role based auth or a separate table.
    // Let's stick to basic role for MVP.
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
