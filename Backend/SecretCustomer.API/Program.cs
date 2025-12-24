using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using SecretCustomer.Data.Repositories;
using SecretCustomer.Services.Helpers;
using SecretCustomer.Services.Services;
using SecretCustomer.API.Middleware;
using SecretCustomer.API.Filters;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is missing");

builder.Services.AddSingleton(new JwtHelper(
    secretKey,
    jwtSettings["Issuer"] ?? "SecretCustomerAPI",
    jwtSettings["Audience"] ?? "SecretCustomerClient",
    int.Parse(jwtSettings["ExpirationMinutes"] ?? "1440")
));

// Authentication - Cookie for MVC, JWT for API
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "Cookies";
    })
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;

        // API istekleri için redirect yerine 401/403 döndür
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"message\":\"Oturum süresi doldu. Lütfen tekrar giriş yapın.\"}");
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"message\":\"Bu işlem için yetkiniz bulunmuyor.\"}");
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

// Authorization Handler
builder.Services.AddScoped<IAuthorizationHandler, SecretCustomer.API.Authorization.PermissionHandler>();

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Legacy Role-based policies (backward compatibility)
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("TeamManagement", policy =>
        policy.RequireRole("Admin", "TeamLeader"));

    options.AddPolicy("CanEvaluate", policy =>
        policy.RequireRole("Admin", "TeamLeader", "Evaluator"));

    options.AddPolicy("FieldWorkerAccess", policy =>
        policy.RequireRole("Admin", "TeamLeader", "FieldWorker"));

    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());

    // NEW: Dynamic Permission-based policies
    options.AddPolicy("ViewProjects", policy =>
        policy.Requirements.Add(new SecretCustomer.API.Authorization.PermissionRequirement("Projects.View")));

    options.AddPolicy("ManageProjects", policy =>
        policy.Requirements.Add(new SecretCustomer.API.Authorization.PermissionRequirement("Projects.Manage")));

    options.AddPolicy("ViewAssignments", policy =>
        policy.Requirements.Add(new SecretCustomer.API.Authorization.PermissionRequirement("Assignments.View")));

    options.AddPolicy("ManageAssignments", policy =>
        policy.Requirements.Add(new SecretCustomer.API.Authorization.PermissionRequirement("Assignments.Manage")));

    options.AddPolicy("ViewUsers", policy =>
        policy.Requirements.Add(new SecretCustomer.API.Authorization.PermissionRequirement("Users.View")));

    options.AddPolicy("ManageUsers", policy =>
        policy.Requirements.Add(new SecretCustomer.API.Authorization.PermissionRequirement("Users.Manage")));

    options.AddPolicy("ViewReports", policy =>
        policy.Requirements.Add(new SecretCustomer.API.Authorization.PermissionRequirement("Reports.View")));

    options.AddPolicy("ManagePermissions", policy =>
        policy.Requirements.Add(new SecretCustomer.API.Authorization.PermissionRequirement("Permissions.Manage")));
});

// Session support for MVC
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Repository Registration
builder.Services.AddScoped<IChecklistRepository, ChecklistRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<IEvaluationRepository, EvaluationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBranchRepository, BranchRepository>();
builder.Services.AddScoped<IFieldWorkerRepository, FieldWorkerRepository>();
builder.Services.AddScoped<IExcelTemplateRepository, ExcelTemplateRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerPersonnelRepository, CustomerPersonnelRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
builder.Services.AddScoped<IUserPermissionRepository, UserPermissionRepository>();

// Service Registration
builder.Services.AddScoped<IChecklistService, ChecklistService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IEvaluationService, EvaluationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IFieldWorkerService, FieldWorkerService>();
builder.Services.AddScoped<IExcelTemplateService, ExcelTemplateService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICustomerPersonnelService, CustomerPersonnelService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IQRCodeService, QRCodeService>();
builder.Services.AddScoped<IAnswerRepository, AnswerRepository>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IPersonnelService, PersonnelService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IAppSettingsService, AppSettingsService>();
builder.Services.AddScoped<IVisitDetailService, VisitDetailService>();

// Localization Service
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ILocalizationService, LocalizationService>();

// Audit Log Service
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// HttpClient for Python Services
builder.Services.AddHttpClient("ExcelProcessor", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("PythonServices:ExcelProcessorUrl") ?? "http://localhost:8002");
    client.Timeout = TimeSpan.FromMinutes(5);
});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add MVC Controllers with Views
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ModelStateValidationFilter>();
});
// Also add API controllers
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ModelStateValidationFilter>();
});

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    // Production için: sadece admin + diller + temel ayarlar
    // Development için: örnek veriler dahil
    var useProductionSeed = builder.Configuration.GetValue<bool>("UseProductionSeed", false);

    if (useProductionSeed || app.Environment.IsProduction())
    {
        var basePath = app.Environment.ContentRootPath;
        await SeedData.InitializeProductionAsync(context, logger, basePath);
    }
    else
    {
        await SeedData.InitializeAsync(context, logger);
    }
}

// Configure the HTTP request pipeline.

// Exception logging middleware (en başta olmalı)
app.UseExceptionLogging();

app.UseCors("AllowAll");

app.UseHttpsRedirection();

// Enable static files
app.UseStaticFiles();

app.UseRouting();

// Enable session
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Map MVC Controllers (for Views)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map API Controllers
app.MapControllers();

app.Run();

// Make the implicit Program class accessible to integration tests
public partial class Program { }
