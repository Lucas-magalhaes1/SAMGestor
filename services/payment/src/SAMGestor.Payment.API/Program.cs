using Microsoft.EntityFrameworkCore;
using SAMGestor.Payment.Application.Abstractions;
using SAMGestor.Payment.Infrastructure.Messaging.Consumers;
using SAMGestor.Payment.Infrastructure.Messaging.Outbox;
using SAMGestor.Payment.Infrastructure.Messaging.RabbitMq;
using SAMGestor.Payment.Infrastructure.Options;
using SAMGestor.Payment.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("Default")
         ?? "Host=localhost;Port=5432;Database=samgestor_db;Username=sam_user;Password=SuP3rS3nh4!";
var schema = builder.Configuration["DB_SCHEMA"] ?? PaymentDbContext.Schema;

builder.Services.AddDbContext<PaymentDbContext>(opt =>
    opt.UseNpgsql(cs, npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", schema)));

builder.Services.Configure<MercadoPagoOptions>(
    builder.Configuration.GetSection("MercadoPago"));

builder.Services.AddHttpClient("mercadopago", client =>
{
    client.BaseAddress = new Uri("https://api.mercadopago.com/");
});

var mqOpt = new RabbitMqOptions
{
    HostName = builder.Configuration["MessageBus:Host"] ?? "localhost",
    UserName = builder.Configuration["MessageBus:User"] ?? "guest",
    Password = builder.Configuration["MessageBus:Pass"] ?? "guest",
    Exchange = "sam.topic"
};

var linkOpt = new PaymentLinkOptions();
builder.Configuration.GetSection("PaymentLink").Bind(linkOpt);
builder.Services.AddSingleton(linkOpt);

builder.Services.AddSingleton(mqOpt);
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddSingleton<EventPublisher>();


builder.Services.AddScoped<IEventBus, OutboxEventBus>();
builder.Services.AddHostedService<OutboxDispatcher>();
builder.Services.AddHostedService<PaymentRequestedConsumer>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();


var app = builder.Build();

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

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

public partial class Program { }