using System.Diagnostics;
using System.Net.Sockets;
using Grpc.Net.Client;
using GrpcTests.Services;
using NUnit.Framework;

namespace GrpcTests.ClientTest;

[TestFixture]
public class SubProcessServerOnNetwork : TestBase
{
  protected override async Task StartServerAsync()
  {
    Console.WriteLine($"Creating server for localhost");
    await CreateAndStartServer().ConfigureAwait(false);

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
    Console.WriteLine($"Creating client for localhost");
    var client = GreeterClient(out _channel);
    return Task.FromResult(client);
  }

  protected override Task DisposeClientAsync()
  {
    _channel?.Dispose();
    return Task.CompletedTask;
  }

  private GrpcChannel? _channel;
  private Process? _serverProcess;
  private Task? _serverTask;

  private async Task CreateAndStartServer()
  {
    _serverProcess = new Process
    {
      StartInfo = new ProcessStartInfo
      {
        FileName = "GrpcTests.Server.exe",
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
  private static Greeter.GreeterClient GreeterClient(out GrpcChannel channel)
  {
    channel = GrpcChannel.ForAddress("http://localhost:80");

    var client = new Greeter.GreeterClient(channel);
    return client;
  }
}