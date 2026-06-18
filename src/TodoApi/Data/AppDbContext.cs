using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Todo> Todos => Set<Todo>();
}

public static class SeedData
{
    public static void Initialize(AppDbContext db)
    {
        if (db.Todos.Any()) return;

        db.Todos.AddRange(
            new Todo { Title = "Buy groceries",   Description = "Milk, eggs, bread",   IsCompleted = false },
            new Todo { Title = "Read clean code",  Description = "Finish chapter 5",    IsCompleted = true,
                       CompletedAt = DateTime.UtcNow.AddDays(-1) },
            new Todo { Title = "Write unit tests", Description = "Cover all endpoints", IsCompleted = false }
        );
        db.SaveChanges();
    }
}
