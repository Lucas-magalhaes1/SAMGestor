using MediatR;
using FluentValidation;
using SAMGestor.Infrastructure.Extensions;
using SAMGestor.API.Extensions;
using SAMGestor.API.Middlewares;
using SAMGestor.Application.Features.Retreats.Create;
using SAMGestor.Infrastructure.Messaging.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers();

builder.Services
    .AddValidatorsFromAssemblyContaining<CreateRetreatValidator>();

builder.Services
    .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddSwaggerDocumentation();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<PaymentConfirmedConsumer>();


var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

public abstract partial class Program { }