using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
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

// Add Controllers con filtro de cambio de contraseña obligatorio
builder.Services.AddControllers(options =>
{
    options.Filters.Add<TitoRegistry.Api.Filters.MustChangePasswordFilter>();
});

// Configure JWT Authentication
const string devDefaultSecret = "TransporteTitoSuperSecretKeyForJwtAuthenticationEnterprise2026!";
var jwtSecret = builder.Configuration["JwtSettings:Secret"];
if (builder.Environment.IsProduction())
{
    if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32 || jwtSecret == devDefaultSecret)
    {
        throw new InvalidOperationException("FATAL: En entorno de Producción, 'JwtSettings:Secret' debe configurarse mediante variable de entorno con un secreto único y no puede coincidir con la clave por defecto.");
    }
}
else if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
    jwtSecret = devDefaultSecret;
}

var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "TitoRegistryApi";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "TitoRegistryClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
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
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var blacklist = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
            var jti = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
            if (!string.IsNullOrEmpty(jti) && blacklist.IsBlacklisted(jti))
            {
                context.Fail("La sesión ha expirado o el token ha sido revocado.");
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Soporte de Forwarded Headers para Render / Proxies Inversos
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Configure CORS for Frontend (Localhost + Producción Render/Vercel)
var configuredOrigins = builder.Configuration["CorsSettings:AllowedOrigins"]?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>();

var defaultOrigins = new[] { "http://localhost:5173", "http://127.0.0.1:5173" };
var allAllowedOrigins = defaultOrigins.Union(configuredOrigins).Distinct().ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (allAllowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(allAllowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Configure Rate Limiting for Auth particionado por IP remota
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("AuthLimit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
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

// Proxies Inversos (Render)
app.UseForwardedHeaders();

// Security Headers Middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
