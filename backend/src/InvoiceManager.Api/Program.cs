var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "InvoiceManager API", Version = "v1" });
});

var app = builder.Build();

// Swagger enabled for easier evaluation
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
