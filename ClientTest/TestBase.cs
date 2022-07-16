using Grpc.Core;
using GrpcTests.Services;
using JetBrains.Annotations;
using NUnit.Framework;

namespace GrpcTests.ClientTest;

/// <summary>
/// This class defines several test scenario for the client.
/// The duration of the tests can be configures using the <code>TestDuration</code> property.
/// The duration is set to several minutes in an attempt to reproduce ENHANCE_YOUR_CALM errors.
/// </summary>
[PublicAPI]
[TestFixture]
public abstract class TestBase
{
  /// <summary>
  /// Defines the duration of each test in this class.
  /// </summary>
  public readonly TimeSpan TestDuration = TimeSpan.FromSeconds(300);

  /// <summary>
  /// Starts the server used for the tests. When the task is completed, the server should be ready to receive requests.
  /// </summary>
  [OneTimeSetUp]
  protected abstract Task StartServerAsync();

  /// <summary>
  /// Stops the server
  /// </summary>
  [OneTimeTearDown]
  protected abstract Task StopServerAsync();

  protected abstract Task<Greeter.GreeterClient> BuildClientAsync();

  protected abstract Task DisposeClientAsync();


  /// <summary>
  /// Connect to the server and do nothing.
  /// </summary>
  [Test]
  public async Task Idle()
  {
    var _ = await BuildClientAsync();

    await Task.Delay(TestDuration);

    await DisposeClientAsync();
  }

  /// <summary>
  /// Make a first call to the server, wait <value>TestDuration</value> and make a second call.
  /// </summary>
  [Test]
  public async Task IdleBetweenShortCalls()
  {
    var client = await BuildClientAsync();

    Assert.That((await client.SayHelloAsync(new HelloRequest { Name = "Me", })).Message, Is.EqualTo("Hello Me"));

    await Task.Delay(TestDuration);

    Assert.That((await client.SayHelloAsync(new HelloRequest { Name = "Me", })).Message, Is.EqualTo("Hello Me"));

    await DisposeClientAsync();
  }

  /// <summary>
  /// Make a call for which the server will take <value>TestDuration</value> to reply.
  /// </summary>
  [Test]
  public async Task LongCall()
  {
    var client = await BuildClientAsync();


    Assert.That((await client.SayHelloLongAsync(new HelloLongRequest
    {
      Name = "Me",
      WaitingTime = (int)TestDuration.TotalSeconds,
    })).Message, Is.EqualTo("Hello Me"));

    await DisposeClientAsync();
  }

  
  /// <summary>
  /// Send continuously requests to the server. Test lasts <value>TestDuration</value>.
  /// </summary>
  [Test]
  public async Task ShortCalls()
  {
    var client = await BuildClientAsync();

    var delay = Task.Delay(TestDuration);
    var counter = 0ul;
    decimal resetCounter = 0ul;

    const int batchSize = 250;
    while (!delay.IsCompleted)
    {
      for (var i = 0; i < batchSize; i++)
        Assert.That((await client.SayHelloAsync(new HelloRequest { Name = "Me", })).Message, Is.EqualTo("Hello Me"));

      counter++;
      if (counter == ulong.MaxValue)
      {
        counter = 0;
        resetCounter++;
      }
    }

    Console.WriteLine($"{new decimal(batchSize) * counter} requests completed");
    Console.WriteLine(
      $"{(ulong)(batchSize * (counter + resetCounter * ulong.MaxValue) / new decimal(TestDuration.TotalSeconds))} requests/s");

    await DisposeClientAsync();
  }

  
  /// <summary>
  /// Send continuously requests to the server. Test lasts <value>TestDuration</value>.
  /// </summary>
  [Test]
  public async Task ParallelShortCalls()
  {
    var client = await BuildClientAsync();

    var delay = Task.Delay(TestDuration);
    var counter = 0ul;
    decimal resetCounter = 0ul;

    const int batchSize = 10000;
    var tasks = new Task<HelloReply>[batchSize];

    while (!delay.IsCompleted)
    {
      for (var i = 0; i < batchSize; i++)
      {
        tasks[i] = client.SayHelloAsync(new HelloRequest { Name = "Me", }).ResponseAsync;
      }

      for (var i = 0; i < batchSize; i++)
      {
        Assert.That((await tasks[i]).Message, Is.EqualTo("Hello Me"));
      }

      counter++;
      if (counter == ulong.MaxValue)
      {
        counter = 0;
        resetCounter++;
      }
    }

    Console.WriteLine($"{new decimal(batchSize) * counter} requests completed");
    Console.WriteLine(
      $"{(ulong)(batchSize * (counter + resetCounter * ulong.MaxValue) / new decimal(TestDuration.TotalSeconds))} requests/s");

    await DisposeClientAsync();
  }

  
  /// <summary>
  /// Open a stream with the server. Wait for <value>TestDuration</value>. Finish the stream.
  /// </summary>
  [Test]

  public async Task IdleStream()
  {
    var client = await BuildClientAsync();

    using var stream = client.SayHelloStream();


    await Task.Delay(TestDuration);

    await stream.RequestStream.CompleteAsync();

    Assert.That(await stream.ResponseStream.ReadAllAsync().ToListAsync(), Is.Empty);

    await DisposeClientAsync();
  }
  
  /// <summary>
  /// Open a stream with the server. Wait for <value>TestDuration</value>. Send a request and finish the stream.
  /// </summary>
  [Test]

  public async Task DelayedStreamUsage()
  {
    var client = await BuildClientAsync();

    using var stream = client.SayHelloStream();


    await Task.Delay(TestDuration);

    await stream.RequestStream.WriteAsync(new HelloRequest { Name = "Me", });

    await stream.RequestStream.CompleteAsync();

    Assert.That(await stream.ResponseStream.ReadAllAsync().CountAsync(), Is.EqualTo(1));

    await DisposeClientAsync();
  }

  
  /// <summary>
  /// Open a stream with the server. end a long request and finish the stream. The long request lasts for <value>TestDuration</value>.
  /// </summary>
  [Test]
  public async Task DelayedStreamReply()
  {
    var client = await BuildClientAsync();

    using var stream = client.SayHelloLongStream();

    await stream.RequestStream.WriteAsync(new HelloLongRequest
    {
      Name = "Me",
      WaitingTime = (int)TestDuration.TotalSeconds,
    });

    await stream.RequestStream.CompleteAsync();
    
    Assert.That(await stream.ResponseStream.ReadAllAsync().CountAsync(), Is.EqualTo(1));

    await DisposeClientAsync();
  }


}