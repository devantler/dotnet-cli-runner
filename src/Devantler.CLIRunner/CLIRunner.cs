using System.Text;
using CliWrap;
using CliWrap.EventStream;
using Spectre.Console;

namespace Devantler.CLIRunner;

/// <summary>
/// A class to run CLI commands and capture their output.
/// </summary>
public class CLIRunner()
{
  /// <summary>
  /// Run a CLI command and capture its output.
  /// </summary>
  /// <param name="command"></param>
  /// <param name="cancellationToken"></param>
  /// <param name="validation"></param>
  /// <param name="silent"></param>
  /// <returns></returns>
  public static async Task<(int ExitCode, string Result)> RunAsync(Command command, CancellationToken cancellationToken, CommandResultValidation validation = CommandResultValidation.ZeroExitCode, bool silent = false)
  {
    bool isFaulty = false;
    StringBuilder result = new();
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
            _ = result.AppendLine(stdOut.Text);
            break;
          case StandardErrorCommandEvent stdErr:
            if (!silent)
            {
              Console.WriteLine(stdErr.Text);
            }
            _ = result.AppendLine(stdErr.Text);
            break;
          case ExitedCommandEvent exited:
            if (System.Diagnostics.Debugger.IsAttached || Environment.GetEnvironmentVariable("DEBUG") is not null)
              AnsiConsole.MarkupLine($"[bold blue]DEBUG[/] Process exited with code {exited.ExitCode}");
            break;
          default:
            if (cmdEvent is null)
            {
              AnsiConsole.MarkupLine("[bold red]ERROR[/] Command event is 'null'");
              return (1, "");
            }
            break;
        }
      }
    }
    catch
    {
      isFaulty = true;
    }
    return isFaulty ? (1, result.ToString()) : (0, result.ToString());
  }
}
