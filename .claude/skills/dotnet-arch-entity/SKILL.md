---
name: dotnet-arch-entity
description: Guides the implementation of a new entity following the Clean Architecture pattern of this project. Covers all layers from DTO to Database, including EF Core Code First, AutoMapper profiles, Repository pattern, Domain services, and DI registration. Use when creating or modifying entities, adding new tables, or scaffolding CRUD features.
allowed-tools: Read, Grep, Glob, Bash, Write, Edit, Task
---

# .NET Clean Architecture — Entity Implementation Guide

You are an expert assistant that helps developers create or modify entities following the exact architecture patterns of this NNews project. You guide the user through ALL required layers.

## Input

The user will describe the entity to create or modify: `$ARGUMENTS`

Before generating code, read existing files (use Category as primary reference) to match current patterns exactly.

---

## Architecture & Data Flow

```
Controller → Service → Repository → DbContext → PostgreSQL
```

**Mapping chain:** EF Entity ↔ Domain Model ↔ DTO (two AutoMapper profiles per entity)

**Projects:**
- `NNews.DTO` — Public API contracts (DTOs)
- `NNews.Domain` — Entity interfaces, models, enums, services
- `NNews.Infra.Interfaces` — Repository contracts
- `NNews.Infra` — EF Core entities, DbContext, repositories, AutoMapper profiles
- `NNews.Application` — DI registration (Initializer.cs)
- `NNews.API` — Controllers

---

## Step-by-Step Implementation

### Step 1: DTO — `NNews.DTO/{Entity}Info.cs`

```csharp
namespace NNews.DTO
{
    public class {Entity}Info
    {
        public long {Entity}Id { get; set; }
        public string Title { get; set; }
        // Nullable types for optional fields (DateTime?, long?)
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
```

Create `{Entity}InsertedInfo` (no Id) and `{Entity}UpdatedInfo` (with Id) if insert/update shapes differ.

### Step 2: Domain Interface — `NNews.Domain/Entities/Interfaces/I{Entity}Model.cs`

```csharp
namespace NNews.Domain.Entities.Interfaces
{
    public interface I{Entity}Model
    {
        long {Entity}Id { get; }
        string Title { get; }
        DateTime CreatedAt { get; }
        DateTime UpdatedAt { get; }
        // Read-only properties only. Mutations via methods:
        void Update(string title);
    }
}
```

### Step 3: Domain Model — `NNews.Domain/Entities/{Entity}Model.cs`

Key patterns (see `CategoryModel.cs` as reference):
- **Private setters** on all properties
- **Private parameterless constructor** (for mapper)
- **Factory methods:** `Create(...)` for new, `Reconstruct(...)` from persistence
- **Validation** in private `Set{Prop}` methods
- **`UpdateTimestamp()`** called on every mutation
- **`Equals`/`GetHashCode`** by Id

```csharp
using NNews.Domain.Entities.Interfaces;

namespace NNews.Domain.Entities
{
    public class {Entity}Model : I{Entity}Model
    {
        public long {Entity}Id { get; private set; }
        public string Title { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private {Entity}Model() { Title = string.Empty; }

        public {Entity}Model(string title) : this()
        {
            SetTitle(title);
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public static {Entity}Model Create(string title) => new(title);

        public static {Entity}Model Reconstruct(long id, string title, DateTime createdAt, DateTime updatedAt)
            => new() { {Entity}Id = id, Title = title, CreatedAt = createdAt, UpdatedAt = updatedAt };

        public void Update(string title) { SetTitle(title); UpdatedAt = DateTime.UtcNow; }

        private void SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be null or empty.", nameof(title));
            Title = title.Trim();
        }

        public override bool Equals(object? obj) =>
            obj is {Entity}Model other && {Entity}Id != 0 && other.{Entity}Id != 0 && {Entity}Id == other.{Entity}Id;
        public override int GetHashCode() => {Entity}Id.GetHashCode();
    }
}
```

### Step 4: EF Entity — `NNews.Infra/Context/{Entity}.cs`

```csharp
namespace NNews.Infra.Context;

public partial class {Entity}
{
    public long {Entity}Id { get; set; }
    public string Title { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Navigation properties: virtual, collections as new List<>()
    public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
}
```

Convention: `partial class`, public setters, `virtual` navigation properties.

### Step 5: DbContext — Modify `NNews.Infra/Context/NNewsContext.cs`

Add DbSet and configure in `OnModelCreating`:

```csharp
// Add DbSet
public virtual DbSet<{Entity}> {Entity}s { get; set; }

// Inside OnModelCreating:
modelBuilder.Entity<{Entity}>(entity =>
{
    entity.HasKey(e => e.{Entity}Id).HasName("{entities}_pkey");
    entity.ToTable("{entities}");  // snake_case plural

    entity.Property(e => e.{Entity}Id)
        .HasDefaultValueSql("nextval('{entity}_id_seq'::regclass)")
        .HasColumnName("{entity}_id");
    entity.Property(e => e.CreatedAt)
        .HasColumnType("timestamp without time zone")
        .HasColumnName("created_at");
    entity.Property(e => e.UpdatedAt)
        .HasColumnType("timestamp without time zone")
        .HasColumnName("updated_at");
    entity.Property(e => e.Title)
        .HasMaxLength(240)
        .HasColumnName("title");
});
```

Convention: snake_case table/columns, PostgreSQL sequences, `timestamp without time zone`, `DeleteBehavior.ClientSetNull` for FKs.

### Step 6: Migration

```bash
dotnet ef migrations add Add{Entity}Table --project NNews.Infra --startup-project NNews.API
dotnet ef database update --project NNews.Infra --startup-project NNews.API
```

### Step 7: Repository Interface — `NNews.Infra.Interfaces/Repository/I{Entity}Repository.cs`

```csharp
namespace NNews.Infra.Interfaces.Repository
{
    public interface I{Entity}Repository<TModel>
    {
        IEnumerable<TModel> ListAll();
        TModel GetById(int id);
        TModel Insert(TModel entity);
        TModel Update(TModel entity);
        void Delete(int id);
    }
}
```

Convention: Generic `<TModel>`. For pagination: `(IEnumerable<TModel> Items, int TotalCount)` tuples.

### Step 8: Repository Implementation — `NNews.Infra/Repository/{Entity}Repository.cs`

Key patterns (see `CategoryRepository.cs`):
- Inject `NNewsContext` + `IMapper`
- **Reads:** `AsNoTracking()`, map EF → Domain via `_mapper.Map<{Entity}Model>(...)`
- **Insert:** `DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)` for timestamps
- **Update:** Fetch tracked entity, mutate properties, `SaveChanges()`
- **Delete:** Fetch, remove, `SaveChanges()`, throw `KeyNotFoundException` if not found

```csharp
using AutoMapper;
using NNews.Domain.Entities;
using NNews.Domain.Entities.Interfaces;
using NNews.Infra.Context;
using NNews.Infra.Interfaces.Repository;

namespace NNews.Infra.Repository
{
    public class {Entity}Repository : I{Entity}Repository<I{Entity}Model>
    {
        private readonly NNewsContext _context;
        private readonly IMapper _mapper;

        public {Entity}Repository(NNewsContext context, IMapper mapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public IEnumerable<I{Entity}Model> ListAll()
        {
            var entities = _context.{Entity}s.AsNoTracking().OrderBy(e => e.Title).ToList();
            return _mapper.Map<IEnumerable<{Entity}Model>>(entities);
        }

        public I{Entity}Model GetById(int id)
        {
            var entity = _context.{Entity}s.AsNoTracking()
                .FirstOrDefault(e => e.{Entity}Id == id)
                ?? throw new KeyNotFoundException($"{Entity} with ID {id} not found.");
            return _mapper.Map<{Entity}Model>(entity);
        }

        public I{Entity}Model Insert(I{Entity}Model model)
        {
            var entity = _mapper.Map<{Entity}>(model);
            entity.CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            entity.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            _context.{Entity}s.Add(entity);
            _context.SaveChanges();
            return _mapper.Map<{Entity}Model>(entity);
        }

        public I{Entity}Model Update(I{Entity}Model model)
        {
            var existing = _context.{Entity}s.FirstOrDefault(e => e.{Entity}Id == model.{Entity}Id)
                ?? throw new KeyNotFoundException($"{Entity} with ID {model.{Entity}Id} not found.");
            existing.Title = model.Title;
            existing.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            _context.SaveChanges();
            return _mapper.Map<{Entity}Model>(existing);
        }

        public void Delete(int id)
        {
            var entity = _context.{Entity}s.FirstOrDefault(e => e.{Entity}Id == id)
                ?? throw new KeyNotFoundException($"{Entity} with ID {id} not found.");
            _context.{Entity}s.Remove(entity);
            _context.SaveChanges();
        }
    }
}
```

### Step 9: AutoMapper Profiles — `NNews.Infra/Mapping/Profiles/`

**Two profiles per entity:**

**`{Entity}Profile.cs`** (EF Entity ↔ Domain Model):
```csharp
using AutoMapper;
using NNews.Domain.Entities;
using NNews.Infra.Context;

namespace NNews.Infra.Mapping.Profiles
{
    public class {Entity}Profile : Profile
    {
        public {Entity}Profile()
        {
            CreateMap<{Entity}, {Entity}Model>()
                .ConstructUsing(src => {Entity}Model.Reconstruct(
                    src.{Entity}Id, src.Title, src.CreatedAt, src.UpdatedAt));

            CreateMap<{Entity}Model, {Entity}>()
                .ForMember(dest => dest.Articles, opt => opt.Ignore()); // Ignore navigation props
        }
    }
}
```

**`{Entity}DtoProfile.cs`** (Domain Model ↔ DTO):
```csharp
using AutoMapper;
using NNews.Domain.Entities;
using NNews.Domain.Entities.Interfaces;
using NNews.DTO;

namespace NNews.Infra.Mapping.Profiles
{
    public class {Entity}DtoProfile : Profile
    {
        public {Entity}DtoProfile()
        {
            CreateMap<{Entity}Model, {Entity}Info>();
            CreateMap<I{Entity}Model, {Entity}Info>();

            CreateMap<{Entity}Info, {Entity}Model>()
                .ConstructUsing(src => src.{Entity}Id > 0
                    ? {Entity}Model.Reconstruct(src.{Entity}Id, src.Title, src.CreatedAt, src.UpdatedAt)
                    : {Entity}Model.Create(src.Title));
        }
    }
}
```

Convention: `ConstructUsing` with factory methods. `Ignore()` navigation props. Map both concrete and interface.

### Step 10: Service Interface — `NNews.Domain/Services/Interfaces/I{Entity}Service.cs`

```csharp
using NNews.DTO;

namespace NNews.Domain.Services.Interfaces
{
    public interface I{Entity}Service
    {
        IList<{Entity}Info> ListAll();
        {Entity}Info GetById(int id);
        {Entity}Info Insert({Entity}Info entity);
        {Entity}Info Update({Entity}Info entity);
        void Delete(int id);
    }
}
```

Convention: Services receive/return **DTOs**, not domain models.

### Step 11: Service Implementation — `NNews.Domain/Services/{Entity}Service.cs`

Key patterns (see `CategoryService.cs`):
- Inject repository (`I{Entity}Repository<I{Entity}Model>`) + `IMapper`
- Map: DTO → Domain Model → Repository → Domain Model → DTO
- Validation in service, not repository
- Throw `ArgumentException` for invalid input, `InvalidOperationException` for business rules

```csharp
using AutoMapper;
using NNews.Domain.Entities;
using NNews.Domain.Entities.Interfaces;
using NNews.Domain.Services.Interfaces;
using NNews.DTO;
using NNews.Infra.Interfaces.Repository;

namespace NNews.Domain.Services
{
    public class {Entity}Service : I{Entity}Service
    {
        private readonly I{Entity}Repository<I{Entity}Model> _repository;
        private readonly IMapper _mapper;

        public {Entity}Service(I{Entity}Repository<I{Entity}Model> repository, IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public IList<{Entity}Info> ListAll() => _mapper.Map<IList<{Entity}Info>>(_repository.ListAll());
        public {Entity}Info GetById(int id) => _mapper.Map<{Entity}Info>(_repository.GetById(id));

        public {Entity}Info Insert({Entity}Info dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            // Add validation here
            var model = _mapper.Map<{Entity}Model>(dto);
            return _mapper.Map<{Entity}Info>(_repository.Insert(model));
        }

        public {Entity}Info Update({Entity}Info dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            // Add validation here
            var model = _mapper.Map<{Entity}Model>(dto);
            return _mapper.Map<{Entity}Info>(_repository.Update(model));
        }

        public void Delete(int id) => _repository.Delete(id);
    }
}
```

### Step 12: DI Registration — Modify `NNews.Application/Initializer.cs`

Add three entries:

```csharp
// Repository region:
injectDependency(typeof(I{Entity}Repository<I{Entity}Model>), typeof({Entity}Repository), services, scoped);

// AutoMapper region:
services.AddAutoMapper(cfg => { }, typeof({Entity}Profile).Assembly);
services.AddAutoMapper(cfg => { }, typeof({Entity}DtoProfile).Assembly);

// Service region:
injectDependency(typeof(I{Entity}Service), typeof({Entity}Service), services, scoped);
```

### Step 13: Controller — `NNews.API/Controllers/{Entity}Controller.cs`

Key patterns (see `CategoryController.cs`):
- Inject `I{Entity}Service`, `IUserClient`, `ILogger`
- `[Authorize]` on write endpoints, `IUserClient.GetUserInSession()` for auth check
- Error handling: `KeyNotFoundException` → 404, `ArgumentException` → 400, generic → 500
- `CreatedAtAction` for POST responses

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAuth.ACL.Interfaces;
using NNews.Domain.Services.Interfaces;
using NNews.DTO;

namespace NNews.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class {Entity}Controller : ControllerBase
    {
        private readonly I{Entity}Service _service;
        private readonly IUserClient _userClient;
        private readonly ILogger<{Entity}Controller> _logger;

        public {Entity}Controller(I{Entity}Service service, IUserClient userClient, ILogger<{Entity}Controller> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _userClient = userClient ?? throw new ArgumentNullException(nameof(userClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet] [Authorize]
        public IActionResult GetAll() { /* auth + _service.ListAll() */ }

        [HttpGet("{id}")]
        public IActionResult GetById(int id) { /* _service.GetById(id), 404 on KeyNotFound */ }

        [HttpPost] [Authorize]
        public IActionResult Insert([FromBody] {Entity}Info dto) { /* auth + validate + CreatedAtAction */ }

        [HttpPut] [Authorize]
        public IActionResult Update([FromBody] {Entity}Info dto) { /* auth + validate + Ok */ }

        [HttpDelete("{id}")] [Authorize]
        public IActionResult Delete(int id) { /* auth + NoContent, 404 on KeyNotFound */ }
    }
}
```

---

## Checklist

| # | Layer | Action | File |
|---|-------|--------|------|
| 1 | DTO | Create | `NNews.DTO/{Entity}Info.cs` |
| 2 | Domain | Create | `NNews.Domain/Entities/Interfaces/I{Entity}Model.cs` |
| 3 | Domain | Create | `NNews.Domain/Entities/{Entity}Model.cs` |
| 4 | Infra | Create | `NNews.Infra/Context/{Entity}.cs` |
| 5 | Infra | Modify | `NNews.Infra/Context/NNewsContext.cs` |
| 6 | Infra | Run | `dotnet ef migrations add Add{Entity}Table` |
| 7 | Infra.Interfaces | Create | `NNews.Infra.Interfaces/Repository/I{Entity}Repository.cs` |
| 8 | Infra | Create | `NNews.Infra/Repository/{Entity}Repository.cs` |
| 9 | Infra | Create | `NNews.Infra/Mapping/Profiles/{Entity}Profile.cs` |
| 10 | Infra | Create | `NNews.Infra/Mapping/Profiles/{Entity}DtoProfile.cs` |
| 11 | Domain | Create | `NNews.Domain/Services/Interfaces/I{Entity}Service.cs` |
| 12 | Domain | Create | `NNews.Domain/Services/{Entity}Service.cs` |
| 13 | Application | Modify | `NNews.Application/Initializer.cs` (3 registrations) |
| 14 | API | Create | `NNews.API/Controllers/{Entity}Controller.cs` |

## Response Guidelines

1. **Read existing files first** to match current patterns exactly
2. **Follow the order** — DTO → Domain → Infra → Application → API
3. **Use Category** as primary reference (simplest complete example)
4. **Run migrations** after modifying DbContext
5. **Match conventions**: snake_case DB, PascalCase C#, factory methods, private setters
6. **PostgreSQL**: `timestamp without time zone`, `DateTime.SpecifyKind(..., Unspecified)`, sequences
