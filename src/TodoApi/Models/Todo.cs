using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models;

public class Todo
{
    public int    Id          { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title       { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool   IsCompleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}

// DTO used for create / update (no Id exposed)
public class TodoUpsertDto
{
    [Required]
    [MaxLength(200)]
    public string Title       { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool   IsCompleted { get; set; } = false;
}
