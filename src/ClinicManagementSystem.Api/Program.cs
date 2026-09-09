

using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DI DbContext
builder.Services.AddDbContext<ClinicManagementDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ClinicDb")));

// DI Repository
builder.Services.AddScoped<IUserRepository, UserRepository>();


var app = builder.Build();



app.Run();