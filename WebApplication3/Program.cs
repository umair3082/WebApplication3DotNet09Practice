using Microsoft.EntityFrameworkCore;
using WebApplication3;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

const string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization(o => o.AddPolicy("AdminsOnly",
                                  b => b.RequireClaim("admin", "true")));
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      builder =>
                      {
                          builder.AllowAnyHeader().AllowAnyMethod()
                          .AllowAnyOrigin();
                      });
});
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Logging.AddJsonConsole();
// Wait 30 seconds for graceful shutdown.
builder.Host.ConfigureHostOptions(o => o.ShutdownTimeout = TimeSpan.FromSeconds(30));
builder.Services.AddAntiforgery();

var app = builder.Build();
app.UseAntiforgery();
app.UseCors();
app.UseAuthorization();

var message = app.Configuration["HelloKey"] ?? "Config failed!";

app.MapGet("/login", [AllowAnonymous] () => "This endpoint is for all roles.");

app.MapGet("/admin", [Authorize("AdminsOnly")] () =>
                             "The /admin endpoint is for admins only.");

app.MapGet("/", () => message);

app.MapGet("/todoitems", async (TodoDb db) =>
    await db.Todos.ToListAsync());

app.MapGet("/todoitems/complete", async (TodoDb db) =>
    await db.Todos.Where(t => t.IsComplete).ToListAsync());

app.MapGet("/todoitems/{id}", async (int id, TodoDb db) =>
    await db.Todos.FindAsync(id)
        is Todo todo
            ? Results.Ok(todo)
            : Results.NotFound())
    .AddEndpointFilter(async (efiContext, next) =>
    {
        app.Logger.LogInformation("Before first filter");
        var result = await next(efiContext);
        app.Logger.LogInformation("After first filter");
        return result;
    })
    .AddEndpointFilter(async (efiContext, next) =>
    {
        app.Logger.LogInformation(" Before 2nd filter");
        var result = await next(efiContext);
        app.Logger.LogInformation(" After 2nd filter");
        return result;
    })
    .AddEndpointFilter(async (efiContext, next) =>
    {
        app.Logger.LogInformation("     Before 3rd filter");
        var result = await next(efiContext);
        app.Logger.LogInformation("     After 3rd filter");
        return result;
    });

app.MapPost("/todoitems", async (Todo todo, TodoDb db) =>
{
    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/todoitems/{todo.Id}", todo);
});

app.MapPut("/todoitems/{id}", async (int id, Todo inputTodo, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return Results.NotFound();

    todo.Name = inputTodo.Name;
    todo.IsComplete = inputTodo.IsComplete;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/todoitems/{id}", async (int id, TodoDb db) =>
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});

TodoEndpoints.Map(app);

app.Run();
