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
// Configure JWT Bearer authentication (Supabase Auth integration)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Supabase JWT configuration
        options.Authority = builder.Configuration["Supabase:Authority"];
        options.Audience = builder.Configuration["Supabase:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Phase 7: Global exception handling
app.UseMiddleware<ExceptionMiddleware>();

// Phase 4: Tenant resolution middleware (User Story 2)
// Must be before UseAuthentication to ensure tenant context is available
app.UseMiddleware<TenantMiddleware>();

// Phase 5: Authentication & Authorization (User Story 3)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
