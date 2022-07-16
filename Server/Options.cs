using System.Diagnostics;
using CommandLine;

namespace GrpcTests.Server;

public class Options
{
  [Option('s', "socket", Required = true, HelpText = "Defines the socket to listened to.")]
  public string SocketPath { get; set; } =
    Path.Combine(Path.GetTempPath(), $"tmp{Environment.ProcessId}.sock");
}