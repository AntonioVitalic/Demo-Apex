using InvoiceManager.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "InvoiceManager API", Version = "v1" });
});

// SQLite + EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=invoice_manager.db";
    options.UseSqlite(connectionString);
});

var app = builder.Build();

// Create DB on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Swagger enabled for easier evaluation
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
