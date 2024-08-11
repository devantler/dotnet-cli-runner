using System.Collections.Concurrent;
using System.Text;
using CliWrap;
using CliWrap.EventStream;
using Spectre.Console;

namespace Devantler.CLIRunner;

/// <summary>
/// A class to run CLI commands and capture their output.
/// </summary>
public static class CLI
{
  /// <summary>
  /// Run a CLI command and capture its output.
  /// </summary>
  /// <param name="command"></param>
  /// <param name="validation"></param>
  /// <param name="silent"></param>
  /// <param name="includeStdErr"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public static async Task<(int exitCode, string result)> RunAsync(
    Command command,
    CommandResultValidation validation = CommandResultValidation.ZeroExitCode,
    bool silent = false,
    bool includeStdErr = true,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(command);
    bool isFaulty = false;
    ConcurrentQueue<string> messageQueue = new();
    try
    {
      await foreach (var cmdEvent in command.WithValidation(validation).ListenAsync(cancellationToken: cancellationToken))
      {
        switch (cmdEvent)
        {
          case StartedCommandEvent started:
            if (System.Diagnostics.Debugger.IsAttached || Environment.GetEnvironmentVariable("DEBUG") is not null)
              AnsiConsole.MarkupLine($"[bold blue]DEBUG[/] Process started: {started.ProcessId}");
            break;
          case StandardOutputCommandEvent stdOut:
            if (!silent)
            {
              Console.WriteLine(stdOut.Text);
            }
            messageQueue.Enqueue(stdOut.Text);
            break;
          case StandardErrorCommandEvent stdErr:
            if (includeStdErr)
            {
              if (!silent)
              {
                Console.WriteLine(stdErr.Text);
              }
              messageQueue.Enqueue(stdErr.Text);
            }
            break;
          case ExitedCommandEvent exited:
            if (System.Diagnostics.Debugger.IsAttached || Environment.GetEnvironmentVariable("DEBUG") is not null)
              AnsiConsole.MarkupLine($"[bold blue]DEBUG[/] Process exited with code {exited.ExitCode}");
            break;
          default:
            throw new CLIException($"Unsupported event type {cmdEvent.GetType()}"); // This should never happen
        }
      }
    }
#pragma warning disable CA1031 // Do not catch general exception types
    catch
#pragma warning restore CA1031 // Do not catch general exception types
    {
      isFaulty = true;
    }
    StringBuilder result = new();
    while (messageQueue.TryDequeue(out string? message))
    {
      _ = result.AppendLine(message);
    }
    return isFaulty ? (1, result.ToString()) : (0, result.ToString());
  }
}
