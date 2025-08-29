using Microsoft.EntityFrameworkCore;
using SAMGestor.Payment.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("Default")
         ?? "Host=localhost;Port=5432;Database=samgestor_db;Username=sam_user;Password=SuP3rS3nh4!";

var schema = builder.Configuration["DB_SCHEMA"] ?? PaymentDbContext.Schema;

builder.Services.AddDbContext<PaymentDbContext>(opt =>
    opt.UseNpgsql(cs, npg =>
        npg.MigrationsHistoryTable("__EFMigrationsHistory", schema)));

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapGet("/health", async (PaymentDbContext db) =>
    (await db.Database.CanConnectAsync())
        ? Results.Ok(new { status = "ok", service = "payment" })
        : Results.Problem("database unavailable"));

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

// Necessário para testes de integração com WebApplicationFactory<Program>
public partial class Program { }