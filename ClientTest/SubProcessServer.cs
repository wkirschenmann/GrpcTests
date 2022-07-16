using System.Diagnostics;
using System.Net.Sockets;
using Grpc.Net.Client;
using GrpcTests.Services;
using NUnit.Framework;

namespace GrpcTests.ClientTest;

[TestFixture]
public class SubProcessServer : TestBase
{
  protected override async Task StartServerAsync()
  {
    Console.WriteLine($"Creating server for socket {_socketPath}");
    await CreateAndStartServer(_socketPath).ConfigureAwait(false);

  }

  protected override async Task StopServerAsync()
  {
    if (_serverProcess is not null) 
      _serverProcess.StandardInput.Close();

    if (_serverTask is not null) 
      await _serverTask;
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
  private GrpcChannel? _channel;
  private Process? _serverProcess;
  private Task? _serverTask;

  private async Task CreateAndStartServer(string socketPath)
  {
    _serverProcess = new Process
    {
      StartInfo = new ProcessStartInfo
      {
        FileName = "GrpcTests.Server.exe",
        ArgumentList =
        {
          "-s",
          socketPath,
        },
        RedirectStandardError = true,
        RedirectStandardOutput = true,
        RedirectStandardInput = true,
        UseShellExecute = false,
      },
    }; 

    var outputReaderTask = Task.Factory.StartNew(async () =>
    {
      while (!_serverProcess.StandardOutput.EndOfStream)
      {
        Console.WriteLine(await _serverProcess.StandardOutput.ReadLineAsync());
      }
    }, TaskCreationOptions.LongRunning).Unwrap();

    var errorReaderTask = Task.Factory.StartNew(async () =>
    {
      while (!_serverProcess.StandardError.EndOfStream)
      {
        await Console.Error.WriteLineAsync(await _serverProcess.StandardError.ReadLineAsync());
      }
    }, TaskCreationOptions.LongRunning).Unwrap();

    _serverTask = Task.WhenAll(outputReaderTask, errorReaderTask);

    _serverProcess.Start();

    // wait for 5 second to allow the server to start listening on the socket.
     await Task.Delay(5000);
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
          await socket.ConnectAsync(udsEndPoint, token).ConfigureAwait(false);
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