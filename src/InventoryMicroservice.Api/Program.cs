using System.Reflection;
using InventoryMicroservice.Api.DependencyInjection;
using InventoryMicroservice.Api.Interfaces;
using InventoryMicroservice.Api.RabbitMQActions;
using InventoryMicroservice.Common.Requests;
using InventoryMicroservice.Infrastructure.DependencyInjection;
using InventoryMicroservice.Service.DependencyInjection;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING", EnvironmentVariableTarget.Process) ?? "";
var databaseName = Environment.GetEnvironmentVariable("DATABASE_NAME", EnvironmentVariableTarget.Process) ?? "";

builder.Services.AddInventoryInfrastructure(connectionString, databaseName);
builder.Services.AddInventoryServices();

var rabbitUsername = Environment.GetEnvironmentVariable("RABBITMQ_USER", EnvironmentVariableTarget.Process) ?? "";
var rabbitPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASS", EnvironmentVariableTarget.Process) ?? "";

builder.Services.AddRabbitMQCredentials(rabbitUsername, rabbitPassword);

var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST", EnvironmentVariableTarget.Process) ?? "";
var rabbitPort = Environment.GetEnvironmentVariable("RABBITMQ_PORT", EnvironmentVariableTarget.Process) ?? "";

builder.Services.AddRabbitMQEntrypoints(rabbitHost, rabbitPort);

builder.Services.AddKeyedScoped<IRabbitMQAction<CreateInventory>, CreateInventoryAction>(CreateInventoryAction.CommandKey);
builder.Services.AddKeyedScoped<IRabbitMQAction<DeleteInventory>, DeleteInventoryAction>(DeleteInventoryAction.CommandKey);
builder.Services.AddKeyedScoped<IRabbitMQAction<UpdateInventory>, UpdateInventoryAction>(UpdateInventoryAction.CommandKey);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Catalog API",
        Version = "v1",
        Description = "eShopOnWeb – Catalog microservice"
    });

    var xmlName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

builder.Services.AddLogging(config =>
{
  config.AddConsole();
});

builder.Services.AddLogging(config =>
  config.AddConsole()
);

var app = builder.Build();

var shouldShowSwagger = Environment.GetEnvironmentVariable(
    "SHOULD_SHOW_SWAGGER",
    EnvironmentVariableTarget.Process
) ?? "";

if (app.Environment.IsDevelopment() || shouldShowSwagger is "true")
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API v1");
    });
}

app.UseAuthorization();
app.MapControllers();

using var scope = app.Services.CreateScope();
scope.StartRabbitMQEntryPoints();

var shouldSetupDb = Environment.GetEnvironmentVariable("SHOULD_SETUP_DATABASE", EnvironmentVariableTarget.Process) ?? "";
if (shouldSetupDb is "true")
{
    await scope.SetupInventoryDatabase();
    app.Logger.LogInformation("Setup Db");
}
else
    app.Logger.LogInformation("Skip db setup");

await app.RunAsync();

