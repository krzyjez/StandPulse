using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StandPulse.SensorProbe;

internal static class Program
{
  internal const string DefaultPipeName = "StandPulse.Sensor";

  /// <summary>Uruchamia narzędzie testowe klienta named pipe.</summary>
  private static async Task<int> Main(string[] args)
  {
    Console.OutputEncoding = Encoding.UTF8;

    var options = ProbeOptions.Parse(args);
    if (options.ShowHelp)
    {
      _WriteHelp();
      return 0;
    }

    using var cancellation = new CancellationTokenSource();
    Console.CancelKeyPress += (_, eventArgs) =>
    {
      eventArgs.Cancel = true;
      cancellation.Cancel();
    };

    try
    {
      var client = new SensorPipeClient(options.PipeName);

      if (options.Command == "get")
      {
        var snapshot = await client.GetStatusAsync(cancellation.Token);
        _WriteSnapshot(snapshot);
        return 0;
      }

      if (options.Command == "watch")
      {
        var received = 0;
        await foreach (var snapshot in client.WatchAsync(cancellation.Token))
        {
          _WriteSnapshot(snapshot);
          received++;

          if (options.Count.HasValue && received >= options.Count.Value)
            return 0;
        }

        return 0;
      }

      _WriteHelp();
      return 1;
    }
    catch (OperationCanceledException)
    {
      return 0;
    }
    catch (Exception ex) when (ex is IOException or TimeoutException or JsonException)
    {
      Console.Error.WriteLine($"Sensor probe error: {ex.Message}");
      return 2;
    }
  }

  // Wypisuje pomoc narzędzia probe.
  private static void _WriteHelp()
  {
    Console.WriteLine("StandPulse.SensorProbe");
    Console.WriteLine();
    Console.WriteLine("Testowy klient Windows Named Pipe dla StandPulse.SensorHost.");
    Console.WriteLine();
    Console.WriteLine("Użycie:");
    Console.WriteLine("  StandPulse.SensorProbe.exe get");
    Console.WriteLine("  StandPulse.SensorProbe.exe watch");
    Console.WriteLine("  StandPulse.SensorProbe.exe watch --count 3");
    Console.WriteLine("  StandPulse.SensorProbe.exe get --pipe StandPulse.Sensor");
    Console.WriteLine();
    Console.WriteLine("Opcje:");
    Console.WriteLine("  --pipe <name>   Nazwa pipe. Domyślnie: StandPulse.Sensor");
    Console.WriteLine("  --count <n>     Liczba statusów do odczytu w trybie watch.");
  }

  // Wypisuje status w czytelnej formie i zachowuje pełny JSON.
  private static void _WriteSnapshot(SensorSnapshot snapshot)
  {
    var lastSample = snapshot.LastSampleAt?.ToString("O") ?? "-";
    Console.WriteLine($"{DateTimeOffset.Now:HH:mm:ss} sensorStatus={snapshot.SensorStatus} sessionStatus={snapshot.SessionStatus} workPresence={snapshot.WorkPresence} lastSampleAt={lastSample} stableSince={snapshot.StableSince:O}");
  }
}

internal sealed class SensorPipeClient
{
  private static readonly Encoding PipeEncoding = new UTF8Encoding(false);
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
  };

  private readonly string _pipeName;

  public SensorPipeClient(string pipeName)
  {
    _pipeName = pipeName;
  }

  /// <summary>Pobiera aktualny snapshot statusu z hosta.</summary>
  public async Task<SensorSnapshot> GetStatusAsync(CancellationToken cancellationToken)
  {
    await using var pipe = await _ConnectAsync(cancellationToken);
    using var reader = new StreamReader(pipe, PipeEncoding, false, 1024, true);
    await using var writer = new StreamWriter(pipe, PipeEncoding, 1024, true)
    {
      AutoFlush = true
    };

    await writer.WriteLineAsync("""{"command":"get"}""".AsMemory(), cancellationToken);
    return _ParseSnapshot(await reader.ReadLineAsync(cancellationToken));
  }

  /// <summary>Subskrybuje aktualny status i kolejne zmiany stabilnego statusu.</summary>
  public async IAsyncEnumerable<SensorSnapshot> WatchAsync(
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
  {
    await using var pipe = await _ConnectAsync(cancellationToken);
    using var reader = new StreamReader(pipe, PipeEncoding, false, 1024, true);
    await using var writer = new StreamWriter(pipe, PipeEncoding, 1024, true)
    {
      AutoFlush = true
    };

    await writer.WriteLineAsync("""{"command":"watch"}""".AsMemory(), cancellationToken);

    while (!cancellationToken.IsCancellationRequested)
    {
      var line = await reader.ReadLineAsync(cancellationToken);
      if (line is null)
        yield break;

      yield return _ParseSnapshot(line);
    }
  }

  // Otwiera połączenie z named pipe hosta.
  private async Task<NamedPipeClientStream> _ConnectAsync(CancellationToken cancellationToken)
  {
    var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
    await pipe.ConnectAsync(TimeSpan.FromSeconds(5), cancellationToken);
    return pipe;
  }

  // Parsuje odpowiedź hosta z jednej linii JSON.
  private static SensorSnapshot _ParseSnapshot(string? line)
  {
    if (string.IsNullOrWhiteSpace(line))
      throw new InvalidDataException("StandPulse.SensorHost returned an empty response.");

    return JsonSerializer.Deserialize<SensorSnapshot>(line, JsonOptions)
           ?? throw new InvalidDataException("StandPulse.SensorHost returned an invalid response.");
  }
}

internal sealed record SensorSnapshot(
  string Type,
  SensorStatus Status,
  SensorStatus SensorStatus,
  WindowsSessionStatus SessionStatus,
  bool WorkPresence,
  DateTimeOffset? LastSampleAt,
  DateTimeOffset StableSince);

internal enum SensorStatus
{
  Presence,
  Absence,
  SensorNotAvailable,
  Error
}

internal enum WindowsSessionStatus
{
  Active,
  Locked,
  Unknown
}

internal sealed record ProbeOptions(
  string Command,
  string PipeName,
  int? Count,
  bool ShowHelp)
{
  /// <summary>Parsuje argumenty CLI narzędzia testowego.</summary>
  public static ProbeOptions Parse(string[] args)
  {
    if (args.Length == 0 || _IsHelp(args[0]))
      return new ProbeOptions("", Program.DefaultPipeName, null, true);

    var command = args[0].ToLowerInvariant();
    var pipeName = Program.DefaultPipeName;
    int? count = null;

    for (var i = 1; i < args.Length; i++)
    {
      if (string.Equals(args[i], "--pipe", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
      {
        pipeName = args[++i];
        continue;
      }

      if (string.Equals(args[i], "--count", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
      {
        if (!int.TryParse(args[++i], out var parsedCount) || parsedCount <= 0)
          return new ProbeOptions(command, pipeName, null, true);

        count = parsedCount;
        continue;
      }

      return new ProbeOptions(command, pipeName, count, true);
    }

    return command is "get" or "watch"
      ? new ProbeOptions(command, pipeName, count, false)
      : new ProbeOptions(command, pipeName, count, true);
  }

  // Rozpoznaje warianty komendy pomocy.
  private static bool _IsHelp(string arg)
  {
    return arg is "help" or "-help" or "--help" or "/?";
  }
}
