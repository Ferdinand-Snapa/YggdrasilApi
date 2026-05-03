using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using YggdrasilApi.Data;
using YggdrasilApi.GameLogick;
using YggdrasilApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IGraphService, GraphService>();

var app = builder.Build();

// Register all built-in node definitions so NodeRegistry can resolve Node.Definition.
BuiltinNodeDefinitions.RegisterDefaults();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
