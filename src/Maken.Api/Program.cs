using Maken.Api.Middleware;
using Maken.Application;
using Maken.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddHttpContextAccessor(); // Required for TenantContext
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Phase 7: Configure Swagger/OpenAPI with XML comments
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Maken API",
        Version = "v1",
        Description = "Multi-tenant educational platform for Islamic Science Institutes",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Maken Support",
            Email = "support@maken.app"
        }
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Configure JWT Bearer authentication in Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Phase 5: Authentication & Authorization (User Story 3)
// Configure JWT Bearer authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secretKey = builder.Configuration["Jwt:SecretKey"] 
            ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
        var issuer = builder.Configuration["Jwt:Issuer"] ?? "Maken";
        var audience = builder.Configuration["Jwt:Audience"] ?? "Maken";
        
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Configure role-based authorization policies
builder.Services.AddAuthorization(options =>
{
    // Policy: Require authentication for all endpoints by default
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // Policy: PlatformAdmin only (global scope)
    options.AddPolicy("PlatformAdminOnly", policy =>
        policy.RequireRole("PlatformAdmin"));

    // Policy: CompanyAdmin or higher (tenant scope)
    options.AddPolicy("CompanyAdminOrHigher", policy =>
        policy.RequireRole("PlatformAdmin", "CompanyAdmin"));

    // Policy: Instructor or higher (tenant scope)
    options.AddPolicy("InstructorOrHigher", policy =>
        policy.RequireRole("PlatformAdmin", "CompanyAdmin", "Instructor"));

    // Policy: Any authenticated user (all roles)
    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());
});

// Add Application layer services (MediatR, FluentValidation)
builder.Services.AddApplication();

// Add Infrastructure layer services (DbContext, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Seed database in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<Maken.Infrastructure.Persistence.MakenDbContext>();
        var passwordHasher = services.GetRequiredService<Maken.Application.Common.Interfaces.IPasswordHasher>();
        await Maken.Infrastructure.Persistence.MakenDbContextSeed.SeedAsync(context, passwordHasher);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add correlation ID to all requests/responses
app.UseMiddleware<CorrelationIdMiddleware>();

// Phase 7: Global exception handling
app.UseMiddleware<ExceptionMiddleware>();

// Phase 5: Authentication & Authorization (User Story 3)
// Must be before TenantMiddleware so JWT claims are available
app.UseAuthentication();
app.UseAuthorization();

// Phase 4: Tenant resolution middleware (User Story 2)
// Runs after authentication to access JWT claims for tenant ID
app.UseMiddleware<TenantMiddleware>();

app.MapControllers();

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
