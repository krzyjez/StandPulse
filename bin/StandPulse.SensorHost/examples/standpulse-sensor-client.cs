using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace StandPulse;

/// <summary>Prosty klient named pipe do komunikacji ze StandPulse.SensorHost.</summary>
public sealed class StandPulseSensorClient
{
  private static readonly Encoding PipeEncoding = new UTF8Encoding(false);
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
  };

  private readonly string _serverName;
  private readonly string _pipeName;
  private readonly TimeSpan _connectTimeout;

  /// <summary>Tworzy klienta dla lokalnego pipe StandPulse.Sensor; bez działającego hosta połączenie kończy się TimeoutException po connectTimeout (domyślnie 5 s).</summary>
  public StandPulseSensorClient(string pipeName = "StandPulse.Sensor", string serverName = ".", TimeSpan? connectTimeout = null)
  {
    _pipeName = pipeName;
    _serverName = serverName;
    _connectTimeout = connectTimeout ?? TimeSpan.FromSeconds(5);
  }

  /// <summary>Pobiera aktualny snapshot statusu z hosta sensora.</summary>
  public async Task<StandPulseSensorSnapshot> GetStatusAsync(CancellationToken cancellationToken = default)
  {
    using var pipe = await _ConnectAsync(cancellationToken);
    using var reader = new StreamReader(pipe, PipeEncoding, false, 1024, true);
    await using var writer = new StreamWriter(pipe, PipeEncoding, 1024, true)
    {
      AutoFlush = true
    };

    await writer.WriteLineAsync("""{"command":"get"}""".AsMemory(), cancellationToken);

    var line = await reader.ReadLineAsync(cancellationToken);
    return _ParseSnapshot(line);
  }

  /// <summary>Subskrybuje aktualny status i późniejsze zmiany stabilnego statusu.</summary>
  public async IAsyncEnumerable<StandPulseSensorSnapshot> WatchAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    using var pipe = await _ConnectAsync(cancellationToken);
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

  // Otwiera połączenie z lokalnym named pipe.
  private async Task<NamedPipeClientStream> _ConnectAsync(CancellationToken cancellationToken)
  {
    var pipe = new NamedPipeClientStream(
      _serverName,
      _pipeName,
      PipeDirection.InOut,
      PipeOptions.Asynchronous);

    try
    {
      await pipe.ConnectAsync(_connectTimeout, cancellationToken);
      return pipe;
    }
    catch
    {
      pipe.Dispose();
      throw;
    }
  }

  // Parsuje pojedynczą linię JSON ze snapshotem statusu.
  private static StandPulseSensorSnapshot _ParseSnapshot(string? line)
  {
    if (string.IsNullOrWhiteSpace(line))
      throw new InvalidDataException("StandPulse.SensorHost returned an empty response.");

    // Odpowiedź innego typu (np. error) zdeserializowałaby się po cichu jako status presence.
    using (var document = JsonDocument.Parse(line))
    {
      var root = document.RootElement;
      var type = root.TryGetProperty("type", out var typeProperty) ? typeProperty.GetString() : null;
      if (type != "status")
      {
        var message = root.TryGetProperty("message", out var messageProperty) ? messageProperty.GetString() : null;
        throw new InvalidDataException($"StandPulse.SensorHost returned '{type ?? "-"}' instead of status: {message ?? line}");
      }
    }

    var snapshot = JsonSerializer.Deserialize<StandPulseSensorSnapshot>(line, JsonOptions);
    return snapshot ?? throw new InvalidDataException("StandPulse.SensorHost returned an invalid status response.");
  }
}

/// <summary>Snapshot stabilnego stanu obecności publikowany przez StandPulse.SensorHost.</summary>
public sealed record StandPulseSensorSnapshot(
  string Type,
  StandPulseSensorStatus Status,
  StandPulseSensorStatus SensorStatus,
  StandPulseWindowsSessionStatus SessionStatus,
  bool WorkPresence,
  DateTimeOffset? LastSampleAt,
  DateTimeOffset StableSince,
  DateTimeOffset WorkPresenceSince);

/// <summary>Stabilny status obecności udostępniany aplikacjom przez StandPulse.SensorHost.</summary>
public enum StandPulseSensorStatus
{
  Presence,
  Absence,
  SensorNotAvailable,
  Error
}

/// <summary>Status sesji Windows publikowany razem ze statusem sensora.</summary>
public enum StandPulseWindowsSessionStatus
{
  Active,
  Locked,
  Unknown
}
