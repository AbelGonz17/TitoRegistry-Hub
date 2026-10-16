using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

// Configure JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"] 
    ?? "TransporteTitoSuperSecretKeyForJwtAuthenticationEnterprise2026!";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "TitoRegistryApi";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "TitoRegistryClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

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

// Configure Swagger / OpenAPI con soporte para Tokens JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Transporte Tito API",
        Version = "v1",
        Description = "API empresarial para el registro de servicios de transporte, control de comisiones y autenticación por tokens JWT.",
        Contact = new OpenApiContact
        {
            Name = "Transporte Tito - Soporte",
            Url = new Uri("https://github.com/AbelGonz17/TitoRegistry-Hub")
        }
    });

    // Definir esquema de seguridad Bearer Token para Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Encabezado de autorización JWT usando el esquema Bearer. Ejemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement((document) => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });

    // Cargar documentación XML
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Transporte Tito API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Transporte Tito API Documentation";
    });
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

