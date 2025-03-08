using DXLAB_Coworking_Space_Booking_System;
using DxLabCoworkingSpace;
using DxLabCoworkingSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    });

builder.Services.AddAuthorization();

//Dependency Injection LibraryDbContext 
builder.Services.AddDbContext<DxLabCoworkingSpaceContext>(options =>
{
    options.UseSqlServer(connectionString);
});
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
//DI AuthorService

builder.Services.AddScoped<IRoleSevice, RoleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
