// Program.cs
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using FeborBack.Infrastructure.Repositories;
using FeborBack.Application.Services;
using FeborBack.Application.Validators;
using FeborBack.Application.Validators.User;
using FeborBack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

// Alias para evitar conflictos de namespace
using UserServices = FeborBack.Application.Services.User;
using MenuServices = FeborBack.Application.Services.Menu;
using MenuRepositories = FeborBack.Infrastructure.Repositories.Menu;
using AuthorizationServices = FeborBack.Application.Services.Authorization;
using AuthorizationRepositories = FeborBack.Infrastructure.Repositories.Authorization;
using ConfigurationServices = FeborBack.Application.Services.Configuration;
using ConfigurationRepositories = FeborBack.Infrastructure.Repositories.Configuration;
using CourseServices = FeborBack.Application.Services.Courses;
using CourseRepositories = FeborBack.Infrastructure.Repositories.Courses;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────
// ⛑️ 1. DEPENDENCIAS
// ─────────────────────────────────────────────

// HttpContextAccessor (para obtener el usuario actual en servicios)
builder.Services.AddHttpContextAccessor();

// MemoryCache para OTP 2FA
builder.Services.AddMemoryCache();

// Entity Framework y Base de Datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseSnakeCaseNamingConvention());

// Validadores
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateUserValidator>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(FeborBack.Application.Mappings.AutoMapperProfile));

// Repositorios (Dapper)
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();

// Servicios de Autenticación
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Servicios de Gestión de Usuarios
builder.Services.AddScoped<UserServices.IUserManagementService, UserServices.UserManagementService>();
builder.Services.AddScoped<UserServices.IUserSupportService, UserServices.UserSupportService>();

// Servicios de Roles
builder.Services.AddScoped<IRoleService, RoleService>();

// Servicios y Repositorios de Menú
builder.Services.AddScoped<MenuRepositories.IMenuRepository, MenuRepositories.MenuRepository>();
builder.Services.AddScoped<MenuServices.IMenuService, MenuServices.MenuService>();

// Servicios de Autorización basada en Menús
builder.Services.AddScoped<AuthorizationRepositories.IMenuAuthorizationRepository, AuthorizationRepositories.MenuAuthorizationRepository>();
builder.Services.AddScoped<AuthorizationServices.IMenuAuthorizationService, AuthorizationServices.MenuAuthorizationService>();

// Servicios de Ciudades
builder.Services.AddScoped<ICityRepository, CityRepository>();
builder.Services.AddScoped<ICityService, CityService>();

// Servicios de Configuración de Email
builder.Services.AddScoped<ConfigurationRepositories.IEmailConfigRepository, ConfigurationRepositories.EmailConfigRepository>();
builder.Services.AddScoped<ConfigurationServices.IEncryptionService, ConfigurationServices.EncryptionService>();
builder.Services.AddScoped<ConfigurationServices.IEmailConfigService, ConfigurationServices.EmailConfigService>();

// Servicios de Notificaciones por Email
builder.Services.AddScoped<FeborBack.Application.Services.Notifications.IEmailNotificationService, FeborBack.Application.Services.Notifications.EmailNotificationService>();

// Servicios de Cursos
builder.Services.AddScoped<FeborBack.Domain.Interfaces.Courses.ICourseRepository, CourseRepositories.CourseRepository>();
builder.Services.AddScoped<CourseServices.ICourseService, CourseServices.CourseService>();

// Servicio OTP para 2FA
builder.Services.AddSingleton<FeborBack.Application.Services.IOtpService, FeborBack.Application.Services.OtpService>();

// Servicios de reCAPTCHA
builder.Services.AddHttpClient<IReCaptchaService, ReCaptchaService>();

// Database Initializer
builder.Services.AddScoped<DatabaseInitializer>();

// ─────────────────────────────────────────────
// 🔐 2. CONFIGURACIÓN JWT
// ─────────────────────────────────────────────
var jwt = builder.Configuration.GetSection("Jwt");
var secretKey = jwt["SecretKey"]!;
var issuer = jwt["Issuer"];
var audience = jwt["Audience"];

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
    };

    opt.Events = new JwtBearerEvents
    {
        // .NET 9 JsonWebTokenHandler does NOT remap "nameid" → ClaimTypes.NameIdentifier.
        // This event manually adds the mapped claims so all code using ClaimTypes works.
        OnTokenValidated = ctx =>
        {
            if (ctx.Principal?.Identity is System.Security.Claims.ClaimsIdentity identity)
            {
                var nameid = identity.FindFirst("nameid")?.Value;
                if (nameid != null && identity.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) == null)
                    identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, nameid));

                var uniqueName = identity.FindFirst("unique_name")?.Value;
                if (uniqueName != null && identity.FindFirst(System.Security.Claims.ClaimTypes.Name) == null)
                    identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, uniqueName));

                foreach (var roleClaim in identity.FindAll("role").ToList())
                {
                    if (!identity.HasClaim(System.Security.Claims.ClaimTypes.Role, roleClaim.Value))
                        identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roleClaim.Value));
                }
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = ctx =>
        {
            var log = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            log.LogWarning("JWT failed: {Message}", ctx.Exception.Message);
            return Task.CompletedTask;
        }
    };
});

// ─────────────────────────────────────────────
// 📜 3. AUTORIZACIÓN
// ─────────────────────────────────────────────
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("RequireAdminRole", p => p.RequireRole("Admin", "Administrador"));
    opt.AddPolicy("RequireUserRole", p => p.RequireRole("User", "Usuario", "Admin", "Administrador"));
});

// ─────────────────────────────────────────────
// 🌐 4. CORS
// ─────────────────────────────────────────────
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("DevCors", p => p
        .SetIsOriginAllowed(_ => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());

    opt.AddPolicy("ProdCors", p => p
        .WithOrigins(
            "https://virtual.febor.co",
            "http://virtual.febor.co",
            "https://febor.coop",
            "http://febor.coop",
            "https://www.febor.coop",
            "http://www.febor.coop",
            "http://localhost:5173",
            "http://localhost:5174",
            "http://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ─────────────────────────────────────────────
// 🚀 5. MVC + Swagger
// ─────────────────────────────────────────────
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 20_000_000; // 20MB
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FeborBack Administracion API",
        Version = "v1",
        Description = "API de administración Febor"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ─────────────────────────────────────────────
// 🏗️ 6. PIPELINE HTTP
// ─────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    IdentityModelEventSource.ShowPII = true;
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FeborBack Administracion API v1");
        c.RoutePrefix = "swagger";
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });

    // En desarrollo la API sirve los archivos de cursos directamente.
    // En producción Nginx los sirve desde /var/www/febor/cursos.
    var coursesPath = builder.Configuration["Courses:BasePath"] ?? "C:\\Febor\\Cursos";
    Directory.CreateDirectory(coursesPath);
    var coursesProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(coursesPath);
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider   = coursesProvider,
        RequestPath    = "/cursos",
        DefaultFileNames = ["index.html"]
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = coursesProvider,
        RequestPath  = "/cursos"
    });
}

app.UseHttpsRedirection();

app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseCors("DevCors");
}

// Middleware: X-HTTP-Method-Override
app.Use(async (context, next) =>
{
    if (context.Request.Headers.TryGetValue("X-HTTP-Method-Override", out var methodOverride))
    {
        var method = methodOverride.ToString().ToUpper();
        if (method == "PUT" || method == "DELETE" || method == "PATCH")
        {
            context.Request.Method = method;
        }
    }
    await next();
});

// No cachear respuestas de la API (excluye archivos estáticos de cursos)
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/cursos"))
    {
        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ─────────────────────────────────────────────
// 🗄️ 7. INICIALIZACIÓN DE BASE DE DATOS
// ─────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await dbInitializer.InitializeAsync();
}

app.Run();
