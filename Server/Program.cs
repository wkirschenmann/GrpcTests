using Microsoft.AspNetCore.Server.Kestrel.Core;

using CommandLine;
using GrpcTests.Server;
using GrpcTests.Services;
using static System.String;

var socketPath = Empty;

Parser.Default.ParseArguments<Options>(args)
  .WithParsed(o =>
  {
    socketPath = o.SocketPath;
  });


var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();
builder.Logging.AddConsole();
builder.Services.AddGrpc();
builder.WebHost.ConfigureKestrel(options =>
{
  if (socketPath != Empty)
  {
    options.ListenUnixSocket(socketPath, listenOptions =>
    {
      listenOptions.Protocols = HttpProtocols.Http2;
    });
  }

  options.ListenLocalhost(80, listenOptions =>
  {
    listenOptions.Protocols = HttpProtocols.Http2;
  });
});

var app = builder.Build();

app.MapGrpcService<GreeterService>();

await app.StartAsync();

Console.WriteLine("Server started. Starting reading the console.");

await Console.In.ReadToEndAsync();

await app.StopAsync(CancellationToken.None);

