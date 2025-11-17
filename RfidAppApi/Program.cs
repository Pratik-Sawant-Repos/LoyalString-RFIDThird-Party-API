using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using RfidAppApi.Data;
using RfidAppApi.Repositories;
using RfidAppApi.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Configure OpenAPI with NSwag
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "RFID Jewelry Inventory System API";
    config.Version = "v1";
    config.Description = "Multi-tenant RFID Jewelry Inventory Management System API";

    // Add JWT Authentication to OpenAPI
    config.AddSecurity("Bearer", new NSwag.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = NSwag.OpenApiSecurityApiKeyLocation.Header,
        Type = NSwag.OpenApiSecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});

// Configure Entity Framework with SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure file upload limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB
    options.ValueLengthLimit = int.MaxValue;
    options.MemoryBufferThreshold = int.MaxValue;
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("Database");

// Register Services
builder.Services.AddScoped<IRfidService, RfidService>();
builder.Services.AddScoped<IRfidExcelService, RfidExcelService>();
builder.Services.AddScoped<IProductExcelService, ProductExcelService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IClientDatabaseService, ClientDatabaseService>();
builder.Services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IUserFriendlyProductService, UserFriendlyProductService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IStockVerificationService, StockVerificationService>();
builder.Services.AddScoped<IStockTransferService, StockTransferService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Register ClientDbContextFactory
builder.Services.AddScoped<ClientDbContextFactory>();

// Register Admin and Activity Services
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IActivityLoggingService, ActivityLoggingService>();
builder.Services.AddScoped<IAccessControlService, AccessControlService>();
builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();

// Register Master Data Services
builder.Services.AddScoped<IMasterDataService, MasterDataService>();

// Register Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// Register User Profile Service
builder.Services.AddScoped<IUserProfileService, UserProfileService>();

// Register Customer and Quotation Services
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IQuotationService, QuotationService>();

// Register Webhook Service
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddHttpClient();

// Register HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"] ?? "YourSecretKeyHere");

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
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "RfidAppApi",
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"] ?? "RfidAppApi",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Configure CORS with better security
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:4200", "https://yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Use NSwag for OpenAPI documentation
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.Path = "";
        config.DocumentPath = "/swagger/v1/swagger.json";
        config.DocumentTitle = "RFID Jewelry Inventory System API";
    });
}

app.UseHttpsRedirection();

// Configure static files for image serving
app.UseStaticFiles();

// Add global exception handling
app.UseExceptionHandler("/error");

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Add permission middleware for regular users
app.UseMiddleware<RfidAppApi.Middleware.PermissionMiddleware>();

app.MapControllers();

// Add health check endpoint
app.MapHealthChecks("/health");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
    
    // Automatically migrate all client databases on startup
    try
    {
        var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();
        app.Logger.LogInformation("Starting automatic migration of all client databases...");
        
        // Run migration in background to avoid blocking startup
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(5000); // Wait 5 seconds for all services to be ready
                await migrationService.MigrateAllClientDatabasesAsync();
                app.Logger.LogInformation("Successfully completed automatic migration of all client databases");
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "Error during automatic migration of client databases");
            }
        });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to start automatic migration service");
    }
}

app.Run();
