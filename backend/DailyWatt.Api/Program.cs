using DailyWatt.Api.Extensions;
using DailyWatt.Application;
using DailyWatt.Infrastructure;
using DailyWatt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationLayer();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddPermissiveCors();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Swagger temporarily disabled due to .NET 10 compatibility issues
// builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    // Seed demo data in development
    if (app.Environment.IsDevelopment())
    {
        await DbSeeder.SeedDemoDataAsync(scope.ServiceProvider);
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Swagger temporarily disabled due to .NET 10 compatibility issues
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
