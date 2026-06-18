using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TodosController : ControllerBase
{
    private readonly AppDbContext _db;

    public TodosController(AppDbContext db) => _db = db;

    // GET api/todos
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Todo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Todo>>> GetAll(
        [FromQuery] bool? completed = null)
    {
        var query = _db.Todos.AsQueryable();
        if (completed.HasValue)
            query = query.Where(t => t.IsCompleted == completed.Value);

        return Ok(await query.OrderByDescending(t => t.CreatedAt).ToListAsync());
    }

    // GET api/todos/5
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Todo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Todo>> GetById(int id)
    {
        var todo = await _db.Todos.FindAsync(id);
        return todo is null ? NotFound(new { message = $"Todo {id} not found." }) : Ok(todo);
    }

    // POST api/todos
    [HttpPost]
    [ProducesResponseType(typeof(Todo), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Todo>> Create([FromBody] TodoUpsertDto dto)
    {
        var todo = new Todo
        {
            Title        = dto.Title,
            Description  = dto.Description,
            IsCompleted  = dto.IsCompleted,
            CompletedAt  = dto.IsCompleted ? DateTime.UtcNow : null
        };

        _db.Todos.Add(todo);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = todo.Id }, todo);
    }

    // PUT api/todos/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Todo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Todo>> Update(int id, [FromBody] TodoUpsertDto dto)
    {
        var todo = await _db.Todos.FindAsync(id);
        if (todo is null)
            return NotFound(new { message = $"Todo {id} not found." });

        todo.Title       = dto.Title;
        todo.Description = dto.Description;

        if (!todo.IsCompleted && dto.IsCompleted)
            todo.CompletedAt = DateTime.UtcNow;
        else if (todo.IsCompleted && !dto.IsCompleted)
            todo.CompletedAt = null;

        todo.IsCompleted = dto.IsCompleted;

        await _db.SaveChangesAsync();
        return Ok(todo);
    }

    // PATCH api/todos/5/complete
    [HttpPatch("{id:int}/complete")]
    [ProducesResponseType(typeof(Todo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Todo>> MarkComplete(int id)
    {
        var todo = await _db.Todos.FindAsync(id);
        if (todo is null)
            return NotFound(new { message = $"Todo {id} not found." });

        todo.IsCompleted = true;
        todo.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(todo);
    }

    // DELETE api/todos/5
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var todo = await _db.Todos.FindAsync(id);
        if (todo is null)
            return NotFound(new { message = $"Todo {id} not found." });

        _db.Todos.Remove(todo);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // GET api/todos/stats
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Stats()
    {
        var total     = await _db.Todos.CountAsync();
        var completed = await _db.Todos.CountAsync(t => t.IsCompleted);
        return Ok(new
        {
            Total     = total,
            Completed = completed,
            Pending   = total - completed
        });
    }
}
