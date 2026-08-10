using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RetailCommerce.Api.Common;
using RetailCommerce.Application.Common;
using RetailCommerce.Infrastructure;
using RetailCommerce.Infrastructure.Identity;
using RetailCommerce.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

// ---- Infrastructure (DbContext, Identity, JWT token service, Auth service) ----
builder.Services.AddInfrastructure(builder.Configuration);

// ---- Current-user + HTTP context ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ---- Authentication (JWT bearer) ----
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CatalogManagers", p => p.RequireRole(Roles.CatalogManagers))
    .AddPolicy("CustomerManagers", p => p.RequireRole(Roles.CustomerManagers))
    .AddPolicy("UserManagers", p => p.RequireRole(Roles.UserManagers));

// ---- CORS (Angular dev server) ----
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// ---- Controllers + validation ----
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(RetailCommerce.Application.Common.Roles).Assembly);

// ---- Problem details + global exception handling ----
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ---- Swagger (with JWT bearer support) ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Retail Commerce API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste only the JWT access token (no 'Bearer ' prefix).",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        },
    });
});

var app = builder.Build();

app.UseExceptionHandler();

// Applies pending migrations (creates the schema on a brand-new database) and ensures roles +
// a default admin login exist — runs in every environment, not just Development, so a fresh
// deploy against an empty PostgreSQL database is immediately usable.
await DbSeeder.BootstrapAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    await DbSeeder.SeedDemoDataAsync(app.Services);
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Serves the Angular production build when its files are published into wwwroot alongside this
// API (see DEPLOYMENT.md) — lets one process/port serve both the app and the API, so a
// same-origin deployment needs no CORS setup and no separate web server. No-op in local dev,
// where wwwroot doesn't exist and the Angular CLI dev server is used instead.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
