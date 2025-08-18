using Microsoft.EntityFrameworkCore;
using SAMGestor.Notification.Application.Abstractions;
using SAMGestor.Notification.Application.Orchestrators;
using SAMGestor.Notification.Infrastructure.Email;
using SAMGestor.Notification.Infrastructure.Messaging;
using SAMGestor.Notification.Infrastructure.Persistence;
using SAMGestor.Notification.Infrastructure.Repositories;
using SAMGestor.Notification.Infrastructure.Templates;

var builder = WebApplication.CreateBuilder(args);

// DbContext
var cs = builder.Configuration.GetConnectionString("Default")
         ?? "Host=localhost;Port=5432;Database=samgestor_db;Username=sam_user;Password=SuP3rS3nh4!";

builder.Services.AddDbContext<NotificationDbContext>(opt =>
    opt.UseNpgsql(cs, npg =>
        npg.MigrationsHistoryTable("__EFMigrationsHistory", NotificationDbContext.Schema)));

// Options
var smtpOpt = new SmtpOptions();
builder.Configuration.GetSection("Smtp").Bind(smtpOpt);
builder.Services.AddSingleton(smtpOpt);

var mqOpt = new RabbitMqOptions();
builder.Configuration.GetSection("RabbitMq").Bind(mqOpt);

// Override por env (docker-compose pode injetar como MessageBus__Host, etc)
if (builder.Configuration["MessageBus:Host"] is { } host) mqOpt.HostName = host;
if (builder.Configuration["MessageBus:User"] is { } usr) mqOpt.UserName = usr;
if (builder.Configuration["MessageBus:Pass"] is { } pwd) mqOpt.Password = pwd;

builder.Services.AddSingleton(mqOpt);

// DI (Application/Infrastructure)
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddSingleton<ITemplateRenderer, SimpleTemplateRenderer>();
builder.Services.AddSingleton<INotificationChannel, EmailChannel>();
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddSingleton<IEventPublisher, EventPublisher>();
builder.Services.AddScoped<NotificationOrchestrator>();

// Background consumers
builder.Services.AddHostedService<SelectionEventConsumer>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapGet("/health", async (NotificationDbContext db) =>
    (await db.Database.CanConnectAsync())
        ? Results.Ok(new { status = "ok", service = "notification" })
        : Results.Problem("database unavailable"));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
