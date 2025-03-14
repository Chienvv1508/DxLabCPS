using DXLAB_Coworking_Space_Booking_System;
using DxLabCoworkingSpace;
using DxLabCoworkingSpace.Service.Sevices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var key = builder.Configuration.GetSection("Jwt")["key"];
var issuer = builder.Configuration.GetSection("Jwt")["Issuer"];
var audience = builder.Configuration.GetSection("Jwt")["Audience"];

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
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
//Dependency Injection LibraryDbContext
builder.Services.AddDbContext<DxLabCoworkingSpaceContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IRoleSevice, RoleService>();
builder.Services.AddScoped<ISlotService, SlotService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);


// ✅ Cập nhật CORS
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

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();
app.UseCors(builder =>
{
    builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader();
});

app.MapControllers();
app.Run();
