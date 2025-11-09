using System.Text.Json.Serialization;
using MediatR;
using FluentValidation;
using QuestPDF.Infrastructure;
using SAMGestor.Infrastructure.Extensions;
using SAMGestor.API.Extensions;
using SAMGestor.API.Middlewares;
using SAMGestor.Application.Features.Retreats.Create;
using SAMGestor.Infrastructure.Messaging.Consumers;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddValidatorsFromAssemblyContaining<CreateRetreatValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddSwaggerDocumentation();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<PaymentConfirmedConsumer>();
builder.Services.AddHostedService<FamilyGroupCreatedConsumer>();
builder.Services.AddHostedService<FamilyGroupCreateFailedConsumer>();
builder.Services.AddHostedService<ServicePaymentConfirmedConsumer>();

// CORS apenas o front local
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost3000", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000" 
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowLocalhost3000");
app.MapControllers();

app.Run();

public abstract partial class Program { }