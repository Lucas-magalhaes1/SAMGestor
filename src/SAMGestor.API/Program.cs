using MediatR;
using SAMGestor.Infrastructure.Extensions;
using SAMGestor.API.Extensions;
using SAMGestor.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers(); 

builder.Services.AddSwaggerDocumentation();

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Infraestrutura (DbContext, MediatR, UoW, etc.)
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