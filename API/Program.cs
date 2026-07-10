using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using OnlineLearningPlatformApi.Application;
using OnlineLearningPlatformApi.Application.IServices;
using OnlineLearningPlatformApi.Application.MyMapper;
using OnlineLearningPlatformApi.Application.Services;
using OnlineLearningPlatformApi.Domain;
using OnlineLearningPlatformApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình AppSettings
var config = builder.Configuration.Get<AppSettings>();
if (config == null) throw new InvalidOperationException("AppSettings missing.");
builder.Services.AddSingleton(config);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
}
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.EnableSensitiveDataLogging()
           .EnableDetailedErrors()
           .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
    options.ConfigureWarnings(warning =>
        warning.Ignore(CoreEventId.NavigationBaseIncludeIgnored));
});

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.AddAutoMapper(cfg => cfg.LicenseKey = "eyJhbGciOiJSUzI1NiIsImtpZCI6Ikx1Y2t5UGVubnlTb2Z0d2FyZUxpY2Vuc2VLZXkvYmJiMTNhY2I1OTkwNGQ4OWI0Y2IxYzg1ZjA4OGNjZjkiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2x1Y2t5cGVubnlzb2Z0d2FyZS5jb20iLCJhdWQiOiJMdWNreVBlbm55U29mdHdhcmUiLCJleHAiOiIxNzk2MTY5NjAwIiwiaWF0IjoiMTc2NDY1MzgwMSIsImFjY291bnRfaWQiOiIwMTlhZGQ4ODAxNmE3NDllOTNjNzRjOTE1MTcwM2I0YiIsImN1c3RvbWVyX2lkIjoiY3RtXzAxa2JlczA3cDAyODN3c2I3aHNzY25ucmRoIiwic3ViX2lkIjoiLSIsImVkaXRpb24iOiIwIiwidHlwZSI6IjIifQ.FgAdXhDW2NOg4jdFCe0_ybRFo8GseXC6oDmzK1j9SUDDPmX10Dezd_4mItXx7WbaBUcItVN5FW5w-IN0tWFDhsNnC1hQ4ajkzN4Gj8WS2ZCAQNTxKVpCBDXfJbGXWKNc1aZDcbjpE_96_u1xPHEgOnZrDn-V_SNr4PRcQDNVjwP94GF_fJzbBvEsaPwtiOusZEQfEpE80ZdW2_5p9IxOiCirW1S0WYV71gJtq2KBc-O36wUBNPhLiVDmT4SEeRHRwftfLfcuawhK2Ru_hdaJvQocjglZ2YSMFWp67X1uxueRLeiLNV7u9so32QjcjJpg_eH6BAtPQGqKTH_mXTUkiQ", typeof(MapperConfigurationProfile));

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<HttpClient>();

// 2. Đăng ký Services (DI)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IGradedItemService, GradedItemService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IStorageService, CloudinaryStorageService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFirebaseStorageService, FirebaseStorageService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<ILessonService, LessonService>();
builder.Services.AddScoped<ILessonResourceService, LessonResourceService>();
builder.Services.AddScoped<ILessonItemService, LessonItemService>();
builder.Services.AddScoped<IModuleService, ModuleService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IUserLessonProgressService, UserLessonProgressService>();

// 3. Cấu hình API Behavior (Tắt filter mặc định của MVC nếu cần tùy chỉnh validate)
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});



// 5. Cấu hình Authentication & Authorization
var key = Encoding.UTF8.GetBytes(config.SecretToken);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Online Learning Platform API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token JWT theo định dạng: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddSignalR();


if (string.IsNullOrWhiteSpace(config.SecretToken))
{
    throw new InvalidOperationException("AppSettings SecretToken is missing.");
}

builder.Services.AddAuthorization();

var app = builder.Build();

// 6. HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseRouting(); // Cần thiết cho MapHub và Controllers
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // Chỉ map Controllers cho Web API

app.Run();
