using System.Diagnostics;
using CommandLine;
using JetBrains.Annotations;
using static System.String;

namespace GrpcTests.Server;

[UsedImplicitly]
public class Options
{

  [UsedImplicitly]
  [Option('s', "socket", Required = false,
    HelpText = "Defines the socket to listened to. If not defined, only localhost:80 will be used")]
  public string SocketPath { get; set; } = Empty;
}