# Todo API — ASP.NET 8 + CI/CD

A production-ready REST API built with **ASP.NET Core 8**, **Entity Framework Core** (In-Memory DB), and a full **GitHub Actions CI/CD pipeline**.

---

## 📁 Project Structure

```
AspNetCICD/
├── .github/
│   └── workflows/
│       └── ci-cd.yml           ← GitHub Actions pipeline
├── src/
│   └── TodoApi/
│       ├── Controllers/
│       │   └── TodosController.cs   ← Full CRUD + stats
│       ├── Data/
│       │   └── AppDbContext.cs      ← EF Core + seed data
│       ├── Models/
│       │   └── Todo.cs              ← Entity + DTO
│       ├── Program.cs               ← DI, Swagger, middleware
│       ├── appsettings.json
│       └── TodoApi.csproj
├── tests/
│   └── TodoApi.Tests/
│       ├── TodosControllerTests.cs  ← 12 integration tests
│       └── TodoApi.Tests.csproj
├── Dockerfile
├── TodoApi.sln
└── README.md
```

---

## 🚀 Quick Start (Local)

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run the API
```bash
cd src/TodoApi
dotnet run
# → https://localhost:5001  (Swagger UI at root)
```

### Run Tests
```bash
dotnet test TodoApi.sln --verbosity normal
```

### Build Release
```bash
dotnet build TodoApi.sln --configuration Release
dotnet publish src/TodoApi/TodoApi.csproj --configuration Release --output ./publish
```

---

## 🐳 Docker

```bash
# Build image (runs tests inside the build stage)
docker build -t todo-api .

# Run container
docker run -p 8080:8080 todo-api

# Open: http://localhost:8080  (Swagger UI)
```

---

## 🔄 CI/CD Pipeline

The workflow at `.github/workflows/ci-cd.yml` runs on every push to `main` or `develop`:

| Step | Action |
|------|--------|
| Checkout | `actions/checkout@v4` |
| Setup .NET 8 | `actions/setup-dotnet@v4` |
| Cache NuGet | `actions/cache@v4` |
| Restore | `dotnet restore` |
| Build | `dotnet build --configuration Release` |
| Test | `dotnet test` with TRX + coverage |
| Publish | `dotnet publish --output publish/` |
| Upload artifact | `actions/upload-artifact@v4` → **todo-api-release** |

The uploaded artifact `todo-api-release` is a self-contained deployable package downloadable from the GitHub Actions run summary.

---

## 📡 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/todos` | List all (optional `?completed=true/false`) |
| GET | `/api/todos/{id}` | Get by ID |
| POST | `/api/todos` | Create new todo |
| PUT | `/api/todos/{id}` | Update todo |
| PATCH | `/api/todos/{id}/complete` | Mark as complete |
| DELETE | `/api/todos/{id}` | Delete todo |
| GET | `/api/todos/stats` | Get totals (total/completed/pending) |

### Sample Payloads

**POST /api/todos**
```json
{
  "title": "Deploy to production",
  "description": "Run CI/CD pipeline",
  "isCompleted": false
}
```

**Response 201**
```json
{
  "id": 4,
  "title": "Deploy to production",
  "description": "Run CI/CD pipeline",
  "isCompleted": false,
  "createdAt": "2026-06-18T10:30:00Z",
  "completedAt": null
}
```

---

## 🧪 Tests (12 integration tests)

All tests use `WebApplicationFactory<Program>` — the real ASP.NET pipeline runs in-memory.

- `GetAll_ReturnsOk_WithSeededItems`
- `GetAll_WithCompletedFilter_ReturnsOnlyCompleted`
- `GetById_ExistingId_ReturnsOk`
- `GetById_NonExistingId_ReturnsNotFound`
- `Create_ValidDto_Returns201WithTodo`
- `Create_EmptyTitle_Returns400`
- `Create_CompletedTodo_SetsCompletedAt`
- `Update_ExistingId_ReturnsUpdatedTodo`
- `Update_NonExistingId_ReturnsNotFound`
- `MarkComplete_ExistingId_SetsIsCompleted`
- `Delete_ExistingId_Returns204`
- `Delete_NonExistingId_ReturnsNotFound`
- `Stats_ReturnsCorrectCounts`

---

## 🏭 Production Notes

- Swap the **In-Memory DB** for SQL Server / PostgreSQL by changing `UseInMemoryDatabase` to `UseSqlServer` / `UseNpgsql` in `Program.cs`
- The `deploy` job in the workflow is commented out — uncomment and fill in your Azure / AWS credentials
- The Dockerfile uses a **non-root user** for security
