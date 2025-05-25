using Api.Extensions;
using FastEndpoints;
using FastEndpoints.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddPortfolioServices(builder.Configuration);

// Add Swagger/OpenAPI
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "Portfolio IA API";
        s.Version = "v1";
        s.Description = "API para procesamiento de archivos de brokers y análisis de portfolio";
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

app.UseHttpsRedirection();

// Use FastEndpoints
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
    c.Serializer.Options.PropertyNamingPolicy = null; // Mantener nombres originales
});

// Use Portfolio endpoints
app.UsePortfolioEndpoints();

app.Run();