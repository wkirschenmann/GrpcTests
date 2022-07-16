using Grpc.Core;

namespace GrpcTests.Services;

public class GreeterService : Greeter.GreeterBase
{
  public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
  {
    return Task.FromResult(new HelloReply
    {
      Message = "Hello " + request.Name
    });
  }

  public override async Task<HelloReply> SayHelloLong(HelloLongRequest request, ServerCallContext context)
  {
    await Task.Delay(TimeSpan.FromSeconds(request.WaitingTime));

    return new HelloReply
    {
      Message = "Hello " + request.Name
    };
  }

  public override async Task SayHelloStream(IAsyncStreamReader<HelloRequest> requestStream, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
  {
    await foreach (var request in requestStream.ReadAllAsync(CancellationToken.None))
    {
      await responseStream.WriteAsync(new HelloReply
      {
        Message = "Hello " + request.Name
      });
    }
  }

  public override async  Task SayHelloLongStream(IAsyncStreamReader<HelloLongRequest> requestStream, IServerStreamWriter<HelloReply> responseStream,
    ServerCallContext context)
  {
    await foreach (var request in requestStream.ReadAllAsync(CancellationToken.None))
    {
      await Task.Delay(TimeSpan.FromSeconds(request.WaitingTime));

      await responseStream.WriteAsync(new HelloReply
      {
        Message = "Hello " + request.Name
      });
    }
  }
}