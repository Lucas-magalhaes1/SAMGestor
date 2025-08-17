using Microsoft.EntityFrameworkCore;
using SAMGestor.Notification.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("Default")
         ?? "Host=localhost;Port=5432;Database=samgestor_db;Username=sam_user;Password=SuP3rS3nh4!";

var schema = NotificationDbContext.Schema;

builder.Services.AddDbContext<NotificationDbContext>(opt =>
    opt.UseNpgsql(cs, npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", schema))
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", async (NotificationDbContext db) =>
    (await db.Database.CanConnectAsync())
        ? Results.Ok(new { status = "ok" })
        : Results.Problem("database unavailable"));

app.Run();