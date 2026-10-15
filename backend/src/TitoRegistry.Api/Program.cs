using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using TitoRegistry.Application;
using TitoRegistry.Application.Common.Interfaces;
using TitoRegistry.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add Clean Architecture Layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add Controllers
builder.Services.AddControllers();

// Configure CORS for Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TitoRegistry-Hub API",
        Version = "v1",
        Description = "API empresarial para el registro de servicios de empleados, cálculo de comisiones y control de acumulados.",
        Contact = new OpenApiContact
        {
            Name = "Equipo TitoRegistry-Hub",
            Url = new Uri("https://github.com/AbelGonz17/TitoRegistry-Hub")
        }
    });

    // Cargar documentación XML generada a partir de comentarios de código
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Aplicar migraciones pendientes automáticamente al iniciar
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Verificando y aplicando migraciones en PostgreSQL...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Base de datos PostgreSQL sincronizada exitosamente.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al aplicar migraciones automáticas en la base de datos.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TitoRegistry-Hub API v1");
        c.RoutePrefix = "swagger"; // Disponible en http://localhost:PORT/swagger
        c.DocumentTitle = "TitoRegistry API Documentation";
    });
}

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
