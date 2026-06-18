using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TodoApi.Models;
using Xunit;

namespace TodoApi.Tests;

/// <summary>
/// Integration tests — spin up the real ASP.NET pipeline in-memory.
/// No mocking needed: the EF In-Memory DB is re-created per WebApplicationFactory.
/// </summary>
public class TodosControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TodosControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/todos ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOk_WithSeededItems()
    {
        var response = await _client.GetAsync("/api/todos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var todos = await response.Content.ReadFromJsonAsync<List<Todo>>();
        todos.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAll_WithCompletedFilter_ReturnsOnlyCompleted()
    {
        var response = await _client.GetAsync("/api/todos?completed=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var todos = await response.Content.ReadFromJsonAsync<List<Todo>>();
        todos!.Should().OnlyContain(t => t.IsCompleted);
    }

    // ── GET /api/todos/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/todos/1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/todos/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/todos ─────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_Returns201WithTodo()
    {
        var dto = new TodoUpsertDto
        {
            Title       = "Integration test todo",
            Description = "Created by xUnit",
            IsCompleted = false
        };

        var response = await _client.PostAsJsonAsync("/api/todos", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<Todo>();
        created!.Title.Should().Be(dto.Title);
        created.IsCompleted.Should().BeFalse();
        created.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_EmptyTitle_Returns400()
    {
        var dto = new TodoUpsertDto { Title = "" };
        var response = await _client.PostAsJsonAsync("/api/todos", dto);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_CompletedTodo_SetsCompletedAt()
    {
        var dto = new TodoUpsertDto { Title = "Done already", IsCompleted = true };
        var response = await _client.PostAsJsonAsync("/api/todos", dto);
        var created  = await response.Content.ReadFromJsonAsync<Todo>();

        created!.CompletedAt.Should().NotBeNull();
    }

    // ── PUT /api/todos/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task Update_ExistingId_ReturnsUpdatedTodo()
    {
        // First create one
        var create   = await _client.PostAsJsonAsync("/api/todos",
            new TodoUpsertDto { Title = "Before update" });
        var original = await create.Content.ReadFromJsonAsync<Todo>();

        var dto      = new TodoUpsertDto { Title = "After update", IsCompleted = false };
        var response = await _client.PutAsJsonAsync($"/api/todos/{original!.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Todo>();
        updated!.Title.Should().Be("After update");
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync("/api/todos/99999",
            new TodoUpsertDto { Title = "Ghost" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PATCH /api/todos/{id}/complete ──────────────────────────────────────

    [Fact]
    public async Task MarkComplete_ExistingId_SetsIsCompleted()
    {
        var create = await _client.PostAsJsonAsync("/api/todos",
            new TodoUpsertDto { Title = "Mark me done" });
        var todo   = await create.Content.ReadFromJsonAsync<Todo>();

        var response = await _client.PatchAsync($"/api/todos/{todo!.Id}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Todo>();
        updated!.IsCompleted.Should().BeTrue();
        updated.CompletedAt.Should().NotBeNull();
    }

    // ── DELETE /api/todos/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingId_Returns204()
    {
        var create = await _client.PostAsJsonAsync("/api/todos",
            new TodoUpsertDto { Title = "Delete me" });
        var todo   = await create.Content.ReadFromJsonAsync<Todo>();

        var response = await _client.DeleteAsync($"/api/todos/{todo!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's gone
        var verify = await _client.GetAsync($"/api/todos/{todo.Id}");
        verify.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/todos/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/todos/stats ────────────────────────────────────────────────

    [Fact]
    public async Task Stats_ReturnsCorrectCounts()
    {
        var response = await _client.GetAsync("/api/todos/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stats = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        stats.Should().ContainKey("Total");
        stats.Should().ContainKey("Completed");
        stats.Should().ContainKey("Pending");
        stats!["Total"].Should().Be(stats["Completed"] + stats["Pending"]);
    }
}
