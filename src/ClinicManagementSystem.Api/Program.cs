

using ClinicManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DI DbContext
builder.Services.AddDbContext<ClinicManagementDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ClinicDb")));

var app = builder.Build();



app.Run();