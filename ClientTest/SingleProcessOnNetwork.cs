using System.Net.Sockets;
using Grpc.Net.Client;
using GrpcTests.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace GrpcTests.ClientTest;

[TestFixture]
public class SingleProcessOnNetwork : TestBase
{
  protected override async Task StartServerAsync()
  {
    Console.WriteLine($"Creating server for localhost:80");
    _server = await CreateAndStartServer().ConfigureAwait(false);
  }

  protected override async Task StopServerAsync()
  {
    if (_server is not null) 
      await _server.StopAsync().ConfigureAwait(false);
  }

  protected override Task<Greeter.GreeterClient> BuildClientAsync()
  {
    Console.WriteLine($"Creating client for localhost:80");
    var client = GreeterClient(out _channel);
    return Task.FromResult(client);
  }

  protected override Task DisposeClientAsync()
  {
    _channel?.Dispose();
    return Task.CompletedTask;
  }

  private WebApplication? _server;
  private GrpcChannel? _channel;

  private static async Task<WebApplication> CreateAndStartServer()
  {
    var builder = WebApplication.CreateBuilder();
    builder.Logging.AddConsole();
    builder.Services.AddGrpc();
    builder.WebHost.ConfigureKestrel(options =>
    {
      options.ListenLocalhost(80, listenOptions =>
      {
        listenOptions.Protocols = HttpProtocols.Http2;
      });
    });
    //builder.Logging.AddFilter("Grpc", LogLevel.Warning);
    builder.Logging.AddFilter("Microsoft", LogLevel.Warning);

    var server = builder.Build();

    server.MapGrpcService<GreeterService>();

    await server.StartAsync(CancellationToken.None).ConfigureAwait(false);
    return server;
  }
  private static Greeter.GreeterClient GreeterClient(out GrpcChannel channel)
  {
    channel = GrpcChannel.ForAddress("http://localhost:80");

    var client = new Greeter.GreeterClient(channel);
    return client;
  }
}