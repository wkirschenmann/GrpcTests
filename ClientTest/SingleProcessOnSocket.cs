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
public class SingleProcessOnSocket : TestBase
{
  protected override async Task StartServerAsync()
  {
    Console.WriteLine($"Creating server for socket {_socketPath}");
    _server = await CreateAndStartServer(_socketPath);
  }

  protected override async Task StopServerAsync()
  {
    if (_server is not null) 
      await _server.StopAsync().ConfigureAwait(false);
  }

  protected override Task<Greeter.GreeterClient> BuildClientAsync()
  {
    Console.WriteLine($"Creating client for socket {_socketPath}");
    var client = GreeterClient(_socketPath, out _channel);
    return Task.FromResult(client);
  }

  protected override Task DisposeClientAsync()
  {
    _channel?.Dispose();
    return Task.CompletedTask;
  }

  private readonly string _socketPath = GetNewSocketPath();
  private WebApplication? _server;
  private GrpcChannel? _channel;

  private static async Task<WebApplication> CreateAndStartServer(string socketPath)
  {
    var builder = WebApplication.CreateBuilder();
    builder.Logging.AddConsole();
    builder.Services.AddGrpc();
    builder.WebHost.ConfigureKestrel(options =>
    {
      options.ListenUnixSocket(socketPath, listenOptions =>
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
  private static Greeter.GreeterClient GreeterClient(string socketPath, out GrpcChannel channel)
  {
    var socketsHttpHandler = new SocketsHttpHandler
    {
      ConnectCallback = async (_, token) =>
      {
        var udsEndPoint = new UnixDomainSocketEndPoint(socketPath);
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
          await socket.ConnectAsync(udsEndPoint, token);
          return new NetworkStream(socket, true);
        }
        catch
        {
          socket.Dispose();
          throw;
        }
      }
    };

    channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
    {
      HttpHandler = socketsHttpHandler,
      DisposeHttpClient = true,
    });

    var client = new Greeter.GreeterClient(channel);
    return client;
  }
  private static string GetNewSocketPath()
  {
    var socketPath = Path.GetTempFileName();
    File.Delete(socketPath);
    Console.WriteLine($"socket path: {socketPath}");

    Assert.That(File.Exists(socketPath), Is.False);

    return socketPath;
  }


}

