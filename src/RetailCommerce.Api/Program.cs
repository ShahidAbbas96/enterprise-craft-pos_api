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

// WebApplication.CreateBuilder() below resolves IWebHostEnvironment.WebRootPath (and its
// WebRootFileProvider) right when it runs — if wwwroot doesn't exist yet at that exact moment
// (always true in local dev, where the Angular build was never copied in), WebRootPath comes back
// null for the rest of the process's life, breaking both UseStaticFiles() and anything (like the
// upload endpoint below) that reads env.WebRootPath directly. Creating the folder before
// CreateBuilder() runs — not merely before Build() — is what actually avoids that trap, in both
// dev and prod (in prod it's a no-op — publish already created wwwroot).
Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads", "products"));

// Windows launches services with the working directory set to C:\Windows\System32, not the
// exe's own folder — without this, the generic host's default content root (CWD) would be wrong
// under the Service Control Manager, breaking appsettings.json lookup and wwwroot static file
// serving (though not the file logger below, which already used AppContext.BaseDirectory).
// AppContext.BaseDirectory is unaffected by working directory either way, so this is a no-op for
// `dotnet run`/console use too.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = AppContext.BaseDirectory });

// ---- Windows Service integration — a plain `sc.exe create ... binPath=` launch of this exe
// runs the process fine, but without this the app never reports SERVICE_RUNNING back to the
// Service Control Manager, so `sc.exe start`/PowerShell's Start-Service always times out with
// "the service did not respond in a timely fashion" (Win32 error 1053) even though the app is
// actually up. UseWindowsService() only activates when actually launched by SCM (matches the
// "RetailCommercePos" name used by INSTALLATION_GUIDE.md's `sc.exe create`) — it's a no-op for
// `dotnet run`/console use, so local dev is unaffected.
builder.Host.UseWindowsService(options => options.ServiceName = "RetailCommercePos");

// ---- File logging (Warning+) — the console the framework logs to by default is invisible once
// this runs as a Windows Service or its terminal window is closed, and clients only ever see
// GlobalExceptionHandler's generic error body, never the real exception. Logs land in `logs/`
// next to the executable so a reported 500 can actually be diagnosed after the fact.
builder.Logging.AddProvider(new RetailCommerce.Api.Common.FileLoggerProvider(Path.Combine(AppContext.BaseDirectory, "logs")));

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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

// Started *before* the migration/seed work below on purpose: under the Service Control Manager,
// StartAsync() is what performs the SCM handshake (reporting SERVICE_RUNNING) via
// UseWindowsService() above. Nothing acknowledges SCM until this call runs, regardless of how the
// lifetime is configured — so running DbSeeder first (as this used to) left SCM waiting through
// however long migrations/DB-connect took, which is exactly what caused "the service did not
// respond in a timely fashion" (Win32 1053) to keep happening even after adding
// UseWindowsService(). Starting first means Kestrel is already accepting connections while
// migrations run just after — the tiny window where a request could hit an unmigrated schema is
// an acceptable tradeoff against the service failing to start at all.
await app.StartAsync();

// Wrapped because this now runs after StartAsync() — an exception here is outside the request
// pipeline (GlobalExceptionHandler never sees it) and, unhandled, would silently crash the
// process with nothing in logs/*.log explaining why: SCM would show it as having started, then
// stopped, with no record of the real cause. Logging it explicitly here closes that gap.
try
{
    // Applies pending migrations (creates the schema on a brand-new database) and ensures roles +
    // a default admin login exist — runs in every environment, not just Development, so a fresh
    // deploy against an empty PostgreSQL database is immediately usable.
    await DbSeeder.BootstrapAsync(app.Services);

    if (app.Environment.IsDevelopment())
    {
        await DbSeeder.SeedDemoDataAsync(app.Services);
    }
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Startup migration/seed failed — the service will now stop.");
    await app.StopAsync();
    throw;
}

await app.WaitForShutdownAsync();
