using System.Collections.Concurrent;
using System.IO.Pipes;
using System.IO.Ports;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace StandPulse.SensorHost;

internal static class Program
{
  /// <summary>Uruchamia host sensora i czeka na zakończenie procesu.</summary>
  private static async Task Main()
  {
    Console.OutputEncoding = Encoding.UTF8;

    var settings = HostSettings.Load();
    var store = new SensorStateStore();
    var host = new SensorHostService(settings, store);
    using var cancellation = new CancellationTokenSource();

    Console.CancelKeyPress += (_, args) =>
    {
      args.Cancel = true;
      cancellation.Cancel();
    };

    Console.WriteLine("StandPulse.SensorHost");
    Console.WriteLine($"Serial: {settings.Serial.PortName} @ {settings.Serial.BaudRate}");
    Console.WriteLine($"Pipe: {settings.Pipe.Name}");
    Console.WriteLine("Naciśnij Ctrl+C, aby zakończyć.");

    await host.RunAsync(cancellation.Token);
  }
}

internal sealed class SensorHostService
{
  private static readonly JsonSerializerOptions CommandJsonOptions = new(JsonSerializerDefaults.Web);
  private static readonly JsonSerializerOptions MessageJsonOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
  };
  private static readonly Encoding PipeEncoding = new UTF8Encoding(false);

  private readonly HostSettings _settings;
  private readonly SensorStateStore _store;

  public SensorHostService(HostSettings settings, SensorStateStore store)
  {
    _settings = settings;
    _store = store;
  }

  /// <summary>Uruchamia odczyt sensora, kontrolę timeoutu i serwer named pipe.</summary>
  public async Task RunAsync(CancellationToken cancellationToken)
  {
    var serialTask = Task.Factory.StartNew(
      () => _ReadSerialAsync(cancellationToken),
      cancellationToken,
      TaskCreationOptions.LongRunning,
      TaskScheduler.Default).Unwrap();
    var pipeTask = Task.Factory.StartNew(
      () => _RunPipeServer(cancellationToken),
      cancellationToken,
      TaskCreationOptions.LongRunning,
      TaskScheduler.Default);

    var tasks = new[]
    {
      serialTask,
      _WatchSampleTimeoutAsync(cancellationToken),
      pipeTask
    };

    await Task.WhenAll(tasks);
  }

  // Czyta JSON Lines z portu COM i przekazuje próbki do stabilizatora stanu.
  private async Task _ReadSerialAsync(CancellationToken cancellationToken)
  {
    while (!cancellationToken.IsCancellationRequested)
    {
      try
      {
        using var serialPort = new SerialPort(_settings.Serial.PortName, _settings.Serial.BaudRate)
        {
          NewLine = "\n",
          ReadTimeout = 1000
        };

        serialPort.Open();
        Console.WriteLine($"Połączono z {_settings.Serial.PortName}.");

        while (!cancellationToken.IsCancellationRequested)
        {
          try
          {
            var line = serialPort.ReadLine().Trim();
            _HandleSensorLine(line);
          }
          catch (TimeoutException)
          {
            // Timeout odczytu pozwala regularnie sprawdzać token anulowania.
          }
        }
      }
      catch (OperationCanceledException)
      {
        return;
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
      {
        Console.WriteLine($"Sensor niedostępny: {ex.Message}");
        _store.SetTechnicalStatus(SensorStatus.SensorNotAvailable);
        await _DelayBeforeReconnectAsync(cancellationToken);
      }
    }
  }

  // Interpretuje jedną linię z firmware i aktualizuje stan stabilny.
  private void _HandleSensorLine(string line)
  {
    try
    {
      var sample = SensorSample.Parse(line);
      if (sample is null)
        return;

      var presentAtDesk = sample.Present &&
                          sample.DistanceCm.HasValue &&
                          sample.DistanceCm.Value < _settings.Presence.MaxDistanceCm;

      _store.RecordSample(
        presentAtDesk ? SensorStatus.Presence : SensorStatus.Absence,
        TimeSpan.FromSeconds(_settings.Presence.StateChangeDelaySeconds));
    }
    catch (JsonException ex)
    {
      Console.WriteLine($"Błąd parsowania próbki: {ex.Message}");
    }
  }

  // Ustawia stan techniczny, gdy sensor przestaje dostarczać aktualne próbki.
  private async Task _WatchSampleTimeoutAsync(CancellationToken cancellationToken)
  {
    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
    var timeout = TimeSpan.FromSeconds(_settings.Presence.SampleTimeoutSeconds);

    while (await timer.WaitForNextTickAsync(cancellationToken))
      _store.MarkUnavailableIfSampleExpired(timeout);
  }

  // Przyjmuje klientów named pipe w przewidywalnej pętli blokującej.
  private void _RunPipeServer(CancellationToken cancellationToken)
  {
    while (!cancellationToken.IsCancellationRequested)
    {
      var pipe = new NamedPipeServerStream(
        _settings.Pipe.Name,
        PipeDirection.InOut,
        NamedPipeServerStream.MaxAllowedServerInstances,
        PipeTransmissionMode.Byte,
        PipeOptions.None);

      try
      {
        pipe.WaitForConnection();
        _ = Task.Factory.StartNew(
          () => _HandlePipeClientSafely(pipe, cancellationToken),
          cancellationToken,
          TaskCreationOptions.LongRunning,
          TaskScheduler.Default);
      }
      catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
      {
        pipe.Dispose();
        if (!cancellationToken.IsCancellationRequested)
          Console.WriteLine($"Błąd serwera pipe: {ex.Message}");
      }
    }
  }

  // Loguje błędy pojedynczego klienta pipe bez zatrzymywania hosta.
  private void _HandlePipeClientSafely(NamedPipeServerStream pipe, CancellationToken cancellationToken)
  {
    try
    {
      _HandlePipeClient(pipe, cancellationToken);
    }
    catch (Exception ex) when (ex is IOException or ObjectDisposedException or JsonException or InvalidOperationException)
    {
      Console.WriteLine($"Błąd klienta pipe: {ex.Message}");
      pipe.Dispose();
    }
  }

  // Obsługuje pojedyncze połączenie klienta pipe.
  private void _HandlePipeClient(NamedPipeServerStream pipe, CancellationToken cancellationToken)
  {
    using var ownedPipe = pipe;
    using var reader = new StreamReader(ownedPipe, PipeEncoding);
    using var writer = new StreamWriter(ownedPipe, PipeEncoding)
    {
      AutoFlush = true
    };

    var line = reader.ReadLine();
    var command = SensorCommand.Parse(line);
    Console.WriteLine($"Pipe command: {command?.Command ?? "-"}");

    if (command?.Command == "get")
    {
      _WriteStatus(writer, _store.GetSnapshot());
      return;
    }

    if (command?.Command == "watch")
    {
      _WriteStatus(writer, _store.GetSnapshot());
      _WatchStatusChanges(writer, cancellationToken);
      return;
    }

    writer.WriteLine("{\"type\":\"error\",\"message\":\"Unknown command.\"}");
  }

  // Wysyła klientowi kolejne zmiany stabilnego statusu.
  private void _WatchStatusChanges(StreamWriter writer, CancellationToken cancellationToken)
  {
    var subscription = _store.Subscribe();

    try
    {
      while (subscription.Reader.WaitToReadAsync(cancellationToken).AsTask().GetAwaiter().GetResult())
      {
        while (subscription.Reader.TryRead(out var snapshot))
          _WriteStatus(writer, snapshot);
      }
    }
    finally
    {
      subscription.Dispose();
    }
  }

  // Serializuje snapshot do jednej linii JSON.
  private static void _WriteStatus(StreamWriter writer, SensorSnapshot snapshot)
  {
    var json = JsonSerializer.Serialize(snapshot, MessageJsonOptions);
    writer.WriteLine(json);
  }

  // Czeka przed ponowną próbą połączenia z portem COM.
  private static async Task _DelayBeforeReconnectAsync(CancellationToken cancellationToken)
  {
    try
    {
      await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
    catch (OperationCanceledException)
    {
    }
  }

  private sealed record SensorCommand(string Command)
  {
    // Parsuje komendę klienta pipe.
    public static SensorCommand? Parse(string? line)
    {
      if (string.IsNullOrWhiteSpace(line))
        return null;

      return JsonSerializer.Deserialize<SensorCommand>(line, CommandJsonOptions);
    }
  }
}

internal sealed class SensorStateStore
{
  private readonly object _syncRoot = new();
  private readonly ConcurrentDictionary<Guid, ChannelWriter<SensorSnapshot>> _subscriptions = new();

  private SensorSnapshot _snapshot = new(SensorStatus.SensorNotAvailable, null, DateTimeOffset.Now);
  private SensorStatus? _candidateStatus;
  private DateTimeOffset? _candidateSince;
  private DateTimeOffset? _lastSampleAt;

  /// <summary>Pobiera aktualny snapshot stanu sensora.</summary>
  public SensorSnapshot GetSnapshot()
  {
    lock (_syncRoot)
      return _snapshot;
  }

  /// <summary>Rejestruje subskrypcję zmian stabilnego statusu.</summary>
  public SensorSubscription Subscribe()
  {
    var channel = Channel.CreateUnbounded<SensorSnapshot>();
    var id = Guid.NewGuid();
    _subscriptions[id] = channel.Writer;
    return new SensorSubscription(id, channel.Reader, this);
  }

  /// <summary>Aktualizuje stabilny stan na podstawie poprawnej próbki sensora.</summary>
  public void RecordSample(SensorStatus sampleStatus, TimeSpan stateChangeDelay)
  {
    SensorSnapshot? changedSnapshot = null;
    var now = DateTimeOffset.Now;

    lock (_syncRoot)
    {
      _lastSampleAt = now;

      if (_candidateStatus != sampleStatus)
      {
        _candidateStatus = sampleStatus;
        _candidateSince = now;
        return;
      }

      var stableFor = now - (_candidateSince ?? now);
      if (stableFor < stateChangeDelay)
        return;

      if (_snapshot.Status == sampleStatus)
      {
        _snapshot = _snapshot with { LastSampleAt = _lastSampleAt };
        return;
      }

      _snapshot = new SensorSnapshot(sampleStatus, _lastSampleAt, _candidateSince ?? now);
      changedSnapshot = _snapshot;
    }

    if (changedSnapshot is not null)
      _Publish(changedSnapshot);
  }

  /// <summary>Ustawia stan techniczny hosta, gdy nie wynika on z normalnej próbki sensora.</summary>
  public void SetTechnicalStatus(SensorStatus status)
  {
    if (status is SensorStatus.Presence or SensorStatus.Absence)
      throw new ArgumentException("Technical status cannot be a presence state.", nameof(status));

    SensorSnapshot? changedSnapshot = null;
    var now = DateTimeOffset.Now;

    lock (_syncRoot)
    {
      _candidateStatus = null;
      _candidateSince = null;

      if (_snapshot.Status == status)
        return;

      _snapshot = new SensorSnapshot(status, _lastSampleAt, now);
      changedSnapshot = _snapshot;
    }

    if (changedSnapshot is not null)
      _Publish(changedSnapshot);
  }

  /// <summary>Oznacza sensor jako niedostępny, jeśli ostatnia próbka jest zbyt stara.</summary>
  public void MarkUnavailableIfSampleExpired(TimeSpan timeout)
  {
    DateTimeOffset? lastSampleAt;

    lock (_syncRoot)
      lastSampleAt = _lastSampleAt;

    if (lastSampleAt is null)
      return;

    if (DateTimeOffset.Now - lastSampleAt.Value > timeout)
      SetTechnicalStatus(SensorStatus.SensorNotAvailable);
  }

  // Usuwa subskrypcję po zamknięciu połączenia klienta.
  private void _Unsubscribe(Guid id)
  {
    _subscriptions.TryRemove(id, out _);
  }

  // Rozsyła snapshot do aktywnych subskrybentów.
  private void _Publish(SensorSnapshot snapshot)
  {
    foreach (var (id, writer) in _subscriptions)
    {
      if (!writer.TryWrite(snapshot))
        _subscriptions.TryRemove(id, out _);
    }

    Console.WriteLine($"{snapshot.StableSince:HH:mm:ss} status={snapshot.Status}");
  }

  internal sealed class SensorSubscription : IDisposable
  {
    private readonly Guid _id;
    private readonly SensorStateStore _owner;

    public SensorSubscription(Guid id, ChannelReader<SensorSnapshot> reader, SensorStateStore owner)
    {
      _id = id;
      Reader = reader;
      _owner = owner;
    }

    public ChannelReader<SensorSnapshot> Reader { get; }

    /// <summary>Kończy subskrypcję zmian statusu.</summary>
    public void Dispose()
    {
      _owner._Unsubscribe(_id);
    }
  }
}

internal sealed record SensorSnapshot(
  [property: JsonPropertyOrder(1)]
  SensorStatus Status,
  [property: JsonPropertyOrder(2)]
  DateTimeOffset? LastSampleAt,
  [property: JsonPropertyOrder(3)]
  DateTimeOffset StableSince)
{
  [JsonPropertyOrder(0)]
  public string Type => "status";
}

internal enum SensorStatus
{
  Presence,
  Absence,
  SensorNotAvailable,
  Error
}

internal sealed record SensorSample(
  bool Present,
  string TargetStatus,
  int? DistanceCm)
{
  /// <summary>Parsuje linię JSON z firmware sensora.</summary>
  public static SensorSample? Parse(string line)
  {
    using var json = JsonDocument.Parse(line);
    var root = json.RootElement;

    if (!root.TryGetProperty("present", out var presentProperty))
      return null;

    var present = presentProperty.GetBoolean();
    var targetStatus = root.TryGetProperty("targetStatus", out var targetStatusProperty)
      ? targetStatusProperty.GetString() ?? "-"
      : "-";

    int? distanceCm = null;
    if (root.TryGetProperty("distanceCm", out var distanceCmProperty) && distanceCmProperty.ValueKind == JsonValueKind.Number)
      distanceCm = distanceCmProperty.GetInt32();
    else if (root.TryGetProperty("distanceMm", out var legacyDistanceProperty) && legacyDistanceProperty.ValueKind == JsonValueKind.Number)
      distanceCm = legacyDistanceProperty.GetInt32();

    return new SensorSample(present, targetStatus, distanceCm);
  }
}

internal sealed record HostSettings(
  SerialSettings Serial,
  PresenceSettings Presence,
  PipeSettings Pipe)
{
  /// <summary>Wczytuje konfigurację hosta z pliku sensor-settings.json.</summary>
  public static HostSettings Load()
  {
    var defaults = Default;
    var path = Path.Combine(AppContext.BaseDirectory, "sensor-settings.json");

    if (!File.Exists(path))
      return defaults;

    try
    {
      var json = File.ReadAllText(path);
      var loaded = JsonSerializer.Deserialize<HostSettings>(
        json,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

      return loaded?.Normalize() ?? defaults;
    }
    catch
    {
      return defaults;
    }
  }

  private static HostSettings Default => new(
    new SerialSettings("COM4", 115200),
    new PresenceSettings(120, 10, 5),
    new PipeSettings("StandPulse.Sensor"));

  // Uzupełnia brakujące albo niepoprawne wartości konfiguracji.
  private HostSettings Normalize()
  {
    var serial = Serial ?? Default.Serial;
    var presence = Presence ?? Default.Presence;
    var pipe = Pipe ?? Default.Pipe;

    if (string.IsNullOrWhiteSpace(serial.PortName))
      serial = serial with { PortName = Default.Serial.PortName };

    if (serial.BaudRate <= 0)
      serial = serial with { BaudRate = Default.Serial.BaudRate };

    if (presence.MaxDistanceCm <= 0)
      presence = presence with { MaxDistanceCm = Default.Presence.MaxDistanceCm };

    if (presence.StateChangeDelaySeconds <= 0)
      presence = presence with { StateChangeDelaySeconds = Default.Presence.StateChangeDelaySeconds };

    if (presence.SampleTimeoutSeconds <= 0)
      presence = presence with { SampleTimeoutSeconds = Default.Presence.SampleTimeoutSeconds };

    if (string.IsNullOrWhiteSpace(pipe.Name))
      pipe = pipe with { Name = Default.Pipe.Name };

    return new HostSettings(serial, presence, pipe);
  }
}

internal sealed record SerialSettings(
  string PortName,
  int BaudRate);

internal sealed record PresenceSettings(
  int MaxDistanceCm,
  int StateChangeDelaySeconds,
  int SampleTimeoutSeconds);

internal sealed record PipeSettings(string Name);
