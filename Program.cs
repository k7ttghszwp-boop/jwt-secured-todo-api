using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TodoApp.Data;

var builder = WebApplication.CreateBuilder(args);

// -----------------------
// JWT CONFIG
// -----------------------
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key bulunamadı (appsettings.json).");
var jwtIssuer = jwtSection["Issuer"] ?? "MyFirstApi";
var jwtAudience = jwtSection["Audience"] ?? "MyFirstApiUsers";
var expiresMinutes = int.TryParse(jwtSection["ExpiresMinutes"], out var m) ? m : 60;

// -----------------------
// AUTH (JWT)
// -----------------------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// -----------------------
// CORS (tek yer)
// -----------------------
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// -----------------------
// SWAGGER (basit, sorunsuz)
// -----------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -----------------------
// DB (SQLite)
// -----------------------
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseSqlite("Data Source=todos.db"));

var app = builder.Build();

// -----------------------
// MIDDLEWARE SIRASI
// -----------------------
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// -----------------------
// AUTH ENDPOINTS
// -----------------------
var auth = app.MapGroup("/auth");

auth.MapPost("/login", (LoginRequest req) =>
{
    if (req.Username != "admin" || req.Password != "123456")
        return Results.Unauthorized();

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, req.Username),
        new Claim(ClaimTypes.Role, "Admin")
    };

    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
        signingCredentials: creds
    );

    return Results.Ok(new
    {
        access_token = new JwtSecurityTokenHandler().WriteToken(token),
        token_type = "Bearer",
        expires_in = expiresMinutes * 60
    });
})
.AllowAnonymous();

// -----------------------
// TODOS (protected)
// -----------------------
var todos = app.MapGroup("/todos").RequireAuthorization();

todos.MapGet("/", async (
    TodoDbContext db,
    int page = 1,
    int pageSize = 10,
    bool? isCompleted = null,
    string? search = null,
    string sortBy = "id",
    string sortDir = "desc") =>
{
    page = page < 1 ? 1 : page;
    pageSize = pageSize < 1 ? 10 : pageSize;
    pageSize = pageSize > 100 ? 100 : pageSize;

    var query = db.Todos.AsNoTracking().AsQueryable();

    if (isCompleted.HasValue)
        query = query.Where(t => t.IsCompleted == isCompleted.Value);

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        query = query.Where(t => EF.Functions.Like(t.Title, $"%{term}%"));
    }

    query = (sortBy.ToLower(), sortDir.ToLower()) switch
    {
        ("title", "asc") => query.OrderBy(t => t.Title),
        ("title", "desc") => query.OrderByDescending(t => t.Title),
        ("id", "asc") => query.OrderBy(t => t.Id),
        _ => query.OrderByDescending(t => t.Id)
    };

    var totalCount = await query.CountAsync();

    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(t => new TodoDto(t.Id, t.Title ?? "", t.IsCompleted))
        .ToListAsync();

    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

    return Results.Ok(new
    {
        page,
        pageSize,
        totalCount,
        totalPages,
        sortBy,
        sortDir,
        items
    });
});

todos.MapGet("/{id:int}", async (int id, TodoDbContext db) =>
{
    var todo = await db.Todos.AsNoTracking()
        .Where(t => t.Id == id)
        .Select(t => new TodoDto(t.Id, t.Title ?? "", t.IsCompleted))
        .FirstOrDefaultAsync();

    return todo is null ? Results.NotFound() : Results.Ok(todo);
});

todos.MapPost("/", async (CreateTodoDto input, TodoDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(input.Title))
        return Results.BadRequest("Title cannot be empty.");

    var entity = new TodoItem
    {
        Title = input.Title.Trim(),
        IsCompleted = false
    };

    db.Todos.Add(entity);
    await db.SaveChangesAsync();

    return Results.Created($"/todos/{entity.Id}", new TodoDto(entity.Id, entity.Title ?? "", entity.IsCompleted));
});

todos.MapPut("/{id:int}", async (int id, UpdateTodoDto input, TodoDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(input.Title))
        return Results.BadRequest("Title cannot be empty.");

    var todo = await db.Todos.FindAsync(id);
    if (todo is null) return Results.NotFound();

    todo.Title = input.Title.Trim();
    todo.IsCompleted = input.IsCompleted;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

todos.MapDelete("/{id:int}", async (int id, TodoDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null) return Results.NotFound();

    db.Todos.Remove(todo);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

public record LoginRequest(string Username, string Password);
