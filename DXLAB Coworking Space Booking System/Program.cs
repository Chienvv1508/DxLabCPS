using DXLAB_Coworking_Space_Booking_System;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System.Text;
using Hangfire;
using Hangfire.MemoryStorage;
using Nethereum.ABI.Model;
using System.IO;
using Microsoft.Extensions.Options;

using DXLAB_Coworking_Space_Booking_System.Hubs;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
});
builder.Services.AddEndpointsApiExplorer();

// SignalR Service
builder.Services.AddSignalR();

builder.Services.AddSwaggerGen(options =>
{   
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "DXLAB Coworking Space Booking System", Version = "v1" });

    // Thêm Bearer Token vào Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token với cú pháp: Bearer {your_token}"
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
            new string[] {}
        }
    });
});

builder.Services.AddAutoMapper(typeof(AutoMapperProfile));  


builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Giá trị không hợp lệ." : e.ErrorMessage)
            .ToList();

        var response = new ResponseDTO<object>(400, "Lỗi: " + string.Join("; ", errors), null);
        return new BadRequestObjectResult(response);
    };
});
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var key = builder.Configuration.GetSection("Jwt")["key"];
var issuer = builder.Configuration.GetSection("Jwt")["Issuer"];
var audience = builder.Configuration.GetSection("Jwt")["Audience"];

// Đọc các giá trị từ configuration
var providerCrawl = builder.Configuration.GetSection("Network")["providerCrawl"];
var contractAddress = builder.Configuration.GetSection("ContractAddresses:Sepolia:Booking").Value;

// Kiểm tra giá trị null
if (string.IsNullOrEmpty(providerCrawl))
    throw new Exception("providerCrawl is missing in appsettings.json");
if (string.IsNullOrEmpty(contractAddress))
    throw new Exception("ContractAddress is missing in appsettings.json");

// Đọc contract ABI từ file với đường dẫn chính xác
var contractAbiPath = Path.Combine(Directory.GetCurrentDirectory(), "Contracts", "Booking.json");
if (!File.Exists(contractAbiPath))
    throw new Exception($"Contract ABI file not found at: {contractAbiPath}");

// Đọc toàn bộ nội dung file và trích xuất mảng "abi"
var contractAbiJson = File.ReadAllText(contractAbiPath);
var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(contractAbiJson);
var contractAbi = jsonObject["abi"]?.ToString();
if (string.IsNullOrEmpty(contractAbi))
    throw new Exception("ABI not found in Booking.json");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub", //Email
            RoleClaimType = ClaimTypes.Role // "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var userId = context.Principal.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    context.Principal.AddIdentity(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }));
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

//Dependency Injection LibraryDbContext
builder.Services.AddDbContext<DxLabSystemContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ISlotService, SlotService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFacilityService, FacilityService>();
builder.Services.AddScoped<IFaciStatusService, FaciStatusService>();
builder.Services.AddScoped<IUsingFacilytyService, UsingFacilityService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IAreaTypeService, AreaTypeService>();
builder.Services.AddScoped<IAreaService, AreaService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IBookingDetailService, BookingDetailService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<IUsingFacilytyService, UsingFacilityService>();
builder.Services.AddScoped<IFaciStatusService, FaciStatusService>();
builder.Services.AddScoped<ISumaryExpenseService, SumaryExpenseService>();
builder.Services.AddScoped<IAreaTypeCategoryService, AreaTypeCategoryService>();

builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddScoped<IImageServiceDb, ImageServiceDb>();
builder.Services.AddScoped<IDepreciationService, DepreciationService>();
builder.Services.AddScoped<IUltilizationRateService, UltilizationRateService>();
builder.Services.AddScoped<IReportService, ReportService>();


//// Đăng ký LabBookingCrawlerService với các giá trị từ configuration
builder.Services.AddScoped<ILabBookingCrawlerService>(sp =>
    new LabBookingCrawlerService(
        providerCrawl,
        contractAddress,
        contractAbi,
        sp.GetRequiredService<IUnitOfWork>()
    ));

// Đăng ký LabBookingJobService
builder.Services.AddScoped<ILabBookingJobService, LabBookingJobService>();
builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

// Thêm Hangfire với MemoryStorage
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage()); // Dùng MemoryStorage

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 10;                  // Số lượng worker
    options.Queues = new[] { "default" };      // Listening queues: 'default'
    options.ShutdownTimeout = TimeSpan.FromSeconds(30)/*FromSeconds(30)*/; // Shutdown timeout
    options.SchedulePollingInterval = TimeSpan.FromSeconds(10)/*FromSeconds(30)*/; // Schedule polling interval
});

// Cập nhật CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Chỉ định frontend của bạn
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // Cho phép gửi token/cookie
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

// Thêm Hangfire Dashboard
app.UseHangfireDashboard();

// Enpoint SIgnalR cho FE call
app.MapHub<BlogHub>("/blogHub");
app.MapHub<ReportHub>("/reportHub");

// Khởi động job crawl sau khi Hangfire server đã khởi động
//app.Lifetime.ApplicationStarted.Register(() =>
//{
//    using (var scope = app.Services.CreateScope())
//    {
//        var jobService = scope.ServiceProvider.GetRequiredService<ILabBookingJobService>();
//        jobService.ScheduleJob();
//    }
//});
app.MapControllers();
app.Run();
