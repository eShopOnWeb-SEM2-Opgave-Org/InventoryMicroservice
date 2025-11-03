
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(config =>
  config.AddConsole()
);

var app = builder.Build();

await app.RunAsync();

