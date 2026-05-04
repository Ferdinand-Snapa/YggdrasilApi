using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using YggdrasilApi.Data;
using YggdrasilApi.Hubs;
using YggdrasilApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IGraphService, GraphService>();
builder.Services.AddSingleton<ISessionService, SessionService>();

// Add SignalR services
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Map SignalR Hub
app.MapHub<GameSessionHub>("/gamesession");

app.Run();
