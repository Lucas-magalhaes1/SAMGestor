using MediatR;
using FluentValidation;
using SAMGestor.Infrastructure.Extensions;
using SAMGestor.API.Extensions;
using SAMGestor.API.Middlewares;
using SAMGestor.Application.Common.Retreat;
using SAMGestor.Application.Features.Retreats.Create;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers();

builder.Services
    .AddValidatorsFromAssemblyContaining<CreateRetreatValidator>();

builder.Services
    .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddSwaggerDocumentation();
builder.Services.AddInfrastructure(builder.Configuration);

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