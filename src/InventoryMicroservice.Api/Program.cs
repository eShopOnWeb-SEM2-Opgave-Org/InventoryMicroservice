using InventoryMicroservice.Api.DependencyInjection;
using InventoryMicroservice.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING", EnvironmentVariableTarget.Process) ?? "";
var databaseName = Environment.GetEnvironmentVariable("DATABASE_NAME", EnvironmentVariableTarget.Process) ?? "";

builder.Services.AddInventoryInfrastructure(connectionString, databaseName);

var rabbitUsername = Environment.GetEnvironmentVariable("RABBITMQ_USER", EnvironmentVariableTarget.Process) ?? "";
var rabbitPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASS", EnvironmentVariableTarget.Process) ?? "";

builder.Services.AddRabbitMQCredentials(rabbitUsername, rabbitPassword);

var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST", EnvironmentVariableTarget.Process) ?? "";
var rabbitPort = Environment.GetEnvironmentVariable("RABBITMQ_PORT", EnvironmentVariableTarget.Process) ?? "";

builder.Services.AddRabbitMQEntrypoints(rabbitHost, rabbitPort);

builder.Services.AddLogging(config =>
{
  config.AddConsole();
});

builder.Services.AddLogging(config =>
  config.AddConsole()
);

var app = builder.Build();

using var scope = app.Services.CreateScope();

scope.StartRabbitMQEntryPoints();

await app.RunAsync();

