using System.Collections.Concurrent;
using System.IO.Pipes;
using System.IO.Ports;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace StandPulse.SensorHost;

internal static class Program
{
  /// <summary>Uruchamia host sensora i czeka na zakończenie procesu.</summary>
  private static async Task<int> Main(string[] args)
  {
    Console.OutputEncoding = Encoding.UTF8;

    if (_IsDocCommand(args))
    {
      Console.WriteLine(_GetDocumentationPath());
      return 0;
    }

    var debugEnabled = _IsDebugCommand(args);
    if (!_ShouldRun(args) && !debugEnabled)
    {
      _WriteHelp();
      return 0;
    }

    var settings = HostSettings.Load();
    using var debugLogger = debugEnabled ? SensorDebugLogger.Create() : null;
    Action<string>? writeLog = debugLogger is null ? null : debugLogger.WriteLine;
    var store = new SensorStateStore(writeLog);
    var host = new SensorHostService(settings, store, debugLogger);
    using var cancellation = new CancellationTokenSource();

    Console.CancelKeyPress += (_, args) =>
    {
      args.Cancel = true;
      cancellation.Cancel();
    };

    Console.WriteLine("StandPulse.SensorHost");
    Console.WriteLine($"Version: {_GetProductVersion()}");
    Console.WriteLine($"Mode: {(debugEnabled ? "debug" : "run")}");
    if (debugLogger is not null)
      Console.WriteLine($"Log: {debugLogger.Path}");
    Console.WriteLine($"Source: {settings.Source.Mode}");
    if (settings.Source.IsSerial)
      Console.WriteLine($"Serial: {settings.Serial.PortName} @ {settings.Serial.BaudRate}");
    Console.WriteLine($"Pipe: {settings.Pipe.Name}");
    Console.WriteLine("Naciśnij Ctrl+C, aby zakończyć.");

    await host.RunAsync(cancellation.Token);
    return 0;
  }

  // Sprawdza, czy host ma przejść w normalny tryb pracy ciągłej.
  private static bool _ShouldRun(string[] args)
  {
    return args.Length == 1 && string.Equals(args[0], "run", StringComparison.OrdinalIgnoreCase);
  }

  // Sprawdza, czy host ma przejść w tryb pracy ciągłej z diagnostyką.
  private static bool _IsDebugCommand(string[] args)
  {
    return args.Length == 1 && string.Equals(args[0], "debug", StringComparison.OrdinalIgnoreCase);
  }

  // Sprawdza, czy użytkownik pyta o ścieżkę dokumentacji obok exe.
  private static bool _IsDocCommand(string[] args)
  {
    return args.Length == 1 && string.Equals(args[0], "doc", StringComparison.OrdinalIgnoreCase);
  }

  // Zwraca ścieżkę do dokumentacji użytkowej obok opublikowanego programu.
  private static string _GetDocumentationPath()
  {
    return Path.Combine(AppContext.BaseDirectory, "StandPulse.SensorHost.md");
  }

  // Pobiera wersję wpisaną do assembly podczas kompilacji.
  private static string _GetProductVersion()
  {
    var version = Assembly.GetExecutingAssembly()
      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
      ?.InformationalVersion;

    return string.IsNullOrWhiteSpace(version) ? "-" : version;
  }

  // Wypisuje pomoc CLI dla użytkownika i autostartu.
  private static void _WriteHelp()
  {
    Console.WriteLine("StandPulse.SensorHost");
    Console.WriteLine($"Version: {_GetProductVersion()}");
    Console.WriteLine();
    Console.WriteLine("Lokalny host sensora obecności StandPulse.");
    Console.WriteLine("Czyta sensor przez COM, stabilizuje status obecności i udostępnia go lokalnym aplikacjom przez Windows Named Pipe.");
    Console.WriteLine();
    Console.WriteLine("Użycie:");
    Console.WriteLine("  StandPulse.SensorHost.exe run");
    Console.WriteLine("  StandPulse.SensorHost.exe debug");
    Console.WriteLine("  StandPulse.SensorHost.exe help");
    Console.WriteLine("  StandPulse.SensorHost.exe -help");
    Console.WriteLine("  StandPulse.SensorHost.exe --help");
    Console.WriteLine("  StandPulse.SensorHost.exe doc");
    Console.WriteLine();
    Console.WriteLine("Komendy:");
    Console.WriteLine("  run    Uruchamia hosta sensora.");
    Console.WriteLine("  debug  Uruchamia hosta sensora z logowaniem surowych linii i decyzji interpretacji do pliku.");
    Console.WriteLine("  help   Pokazuje tę pomoc. Brak argumentów także pokazuje pomoc.");
    Console.WriteLine("  doc    Wypisuje ścieżkę do dokumentacji Markdown obok programu.");
    Console.WriteLine();
    Console.WriteLine("Konfiguracja:");
    Console.WriteLine("  sensor-settings.json");
    Console.WriteLine();
    Console.WriteLine("Domyślny pipe:");
    Console.WriteLine("  StandPulse.Sensor");
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
  private readonly SensorDebugLogger? _debugLogger;
  private int _consecutiveSensorErrors;
  private int _startupInvalidLineCount;
  private bool _receivedValidSensorMessage;

  public SensorHostService(HostSettings settings, SensorStateStore store, SensorDebugLogger? debugLogger)
  {
    _settings = settings;
    _store = store;
    _debugLogger = debugLogger;
  }

  /// <summary>Uruchamia odczyt sensora, kontrolę timeoutu i serwer named pipe.</summary>
  public async Task RunAsync(CancellationToken cancellationToken)
  {
    var sourceTask = Task.Factory.StartNew(
      () => _ReadSamplesAsync(cancellationToken),
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
      sourceTask,
      _WatchSampleTimeoutAsync(cancellationToken),
      pipeTask
    };

    await Task.WhenAll(tasks);
  }

  // Wybiera realny port COM albo fake źródło próbek.
  private Task _ReadSamplesAsync(CancellationToken cancellationToken)
  {
    return _settings.Source.IsFake
      ? _ReadFakeSamplesAsync(cancellationToken)
      : _ReadSerialAsync(cancellationToken);
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
            var rawLine = serialPort.ReadLine();
            _HandleSensorLine(rawLine);
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
        _WriteOperationalLog($"Sensor niedostępny: {ex.Message}");
        _store.SetTechnicalStatus(SensorStatus.SensorNotAvailable);
        await _DelayBeforeReconnectAsync(cancellationToken);
      }
    }
  }

  // Interpretuje jedną linię z firmware i aktualizuje stan stabilny.
  private void _HandleSensorLine(string rawLine)
  {
    var line = rawLine.Trim();
    _WriteDebug($"RAW length={rawLine.Length} text=\"{_EscapeForLog(rawLine)}\" hex={_FormatCodeUnits(rawLine)}");

    if (!_LooksLikeJson(line))
    {
      _WriteDebug($"LINE ignored=true reason=non-json-startup-noise source=\"{_EscapeForLog(line)}\"");
      _RecordStartupInvalidLine("non-json-startup-noise");
      return;
    }

    try
    {
      var firmwareEvent = SensorFirmwareEvent.Parse(line);
      if (firmwareEvent is not null)
      {
        _receivedValidSensorMessage = true;
        _startupInvalidLineCount = 0;
        _WriteDebug($"EVENT event={firmwareEvent.Event}");
        if (firmwareEvent.Event == "radar_read_error")
        {
          _consecutiveSensorErrors++;
          _WriteOperationalLog($"Błąd firmware sensora: {_consecutiveSensorErrors}/{_settings.Presence.MaxConsecutiveSensorErrors}");

          if (_consecutiveSensorErrors >= _settings.Presence.MaxConsecutiveSensorErrors)
            _store.SetTechnicalStatus(SensorStatus.Error);

          _WriteDebug($"ERROR_COUNTER value={_consecutiveSensorErrors} max={_settings.Presence.MaxConsecutiveSensorErrors} state={_FormatDebugState()}");
        }
        else
        {
          _WriteDebug("EVENT ignored=true");
        }

        return;
      }

      var sample = SensorSample.Parse(line);
      if (sample is null)
      {
        _WriteDebug("SAMPLE ignored=true reason=missing-present");
        return;
      }

      _receivedValidSensorMessage = true;
      _startupInvalidLineCount = 0;
      _consecutiveSensorErrors = 0;
      var presentAtDesk = sample.Present &&
                          sample.DistanceCm.HasValue &&
                          sample.DistanceCm.Value < _settings.Presence.MaxDistanceCm;
      var sampleStatus = presentAtDesk ? SensorStatus.Presence : SensorStatus.Absence;
      var sampleDecision = new SensorSampleDecision(
        sample.Present,
        sample.TargetStatus,
        sample.DistanceCm,
        _settings.Presence.MaxDistanceCm,
        sampleStatus);

      _WriteDebug(
        $"SAMPLE present={sample.Present} targetStatus={sample.TargetStatus} distanceCm={sample.DistanceCm?.ToString() ?? "-"} maxDistanceCm={_settings.Presence.MaxDistanceCm} decision={sampleStatus}");

      _store.RecordSample(
        sampleStatus,
        sampleDecision,
        TimeSpan.FromSeconds(_settings.Presence.StateChangeDelaySeconds));
      _WriteDebug($"STATE {_FormatDebugState()}");
    }
    catch (JsonException ex)
    {
      _WriteDebug($"PARSE_ERROR message=\"{_EscapeForLog(ex.Message)}\" source=\"{_EscapeForLog(line)}\"");
      if (_receivedValidSensorMessage)
        _WriteOperationalLog($"Błąd parsowania próbki: {ex.Message}");
      else
      {
        _WriteDebug("PARSE_ERROR ignored=true reason=before-first-valid-message");
        _RecordStartupInvalidLine("json-parse-error");
      }
    }
  }

  // Pilnuje, żeby nieskończone śmieci startowe sensora nie zostawiły hosta bez diagnozy.
  private void _RecordStartupInvalidLine(string reason)
  {
    if (_receivedValidSensorMessage)
      return;

    _startupInvalidLineCount++;
    _WriteDebug($"STARTUP_INVALID_LINE count={_startupInvalidLineCount} max={_settings.Presence.MaxStartupInvalidLines} reason={reason}");

    if (_startupInvalidLineCount == _settings.Presence.MaxStartupInvalidLines)
    {
      _WriteOperationalLog($"Sensor wysyła niepoprawne dane startowe: {_startupInvalidLineCount}/{_settings.Presence.MaxStartupInvalidLines}");
      _store.SetTechnicalStatus(SensorStatus.Error);
    }
  }

  // Odróżnia właściwe linie JSON od boot-logu ESP32 i fragmentów złapanych przy starcie portu.
  private static bool _LooksLikeJson(string line)
  {
    return line.StartsWith('{');
  }

  // Generuje przewidywalne próbki bez fizycznego sensora.
  private async Task _ReadFakeSamplesAsync(CancellationToken cancellationToken)
  {
    _WriteOperationalLog("Używam fake źródła próbek sensora.");

    while (!cancellationToken.IsCancellationRequested)
    {
      foreach (var sample in _settings.Fake.Samples)
      {
        var repeat = Math.Max(1, sample.Repeat);
        for (var i = 0; i < repeat; i++)
        {
          _HandleSensorLine(_CreateFakeLine(sample));
          await Task.Delay(_settings.Fake.IntervalMilliseconds, cancellationToken);
        }
      }
    }
  }

  // Tworzy linię JSON zgodną z firmware.
  private static string _CreateFakeLine(FakeSensorSampleSettings sample)
  {
    if (sample.RawLine is not null)
      return sample.RawLine;

    if (!string.IsNullOrWhiteSpace(sample.Event))
    {
      return JsonSerializer.Serialize(
        new
        {
          @event = sample.Event,
          millis = Environment.TickCount64
        },
        CommandJsonOptions);
    }

    return JsonSerializer.Serialize(
      new
      {
        present = sample.Present,
        targetStatus = sample.TargetStatus,
        distanceCm = sample.DistanceCm,
        radarMode = 2
      },
      CommandJsonOptions);
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
        _WriteOperationalLog($"Błąd serwera pipe: {ex.Message}");
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
      _WriteOperationalLog($"Błąd klienta pipe: {ex.Message}");
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
    _WriteOperationalLog($"Pipe command: {command?.Command ?? "-"}");

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

  // Wypisuje linię diagnostyczną tylko w trybie debug.
  private void _WriteDebug(string message)
  {
    _debugLogger?.WriteLine($"DEBUG {message}");
  }

  // Kieruje zwykłe komunikaty hosta na konsolę albo do pliku w trybie debug.
  private void _WriteOperationalLog(string message)
  {
    if (_debugLogger is not null)
      _debugLogger.WriteLine(message);
    else
      Console.WriteLine(message);
  }

  // Formatuje snapshot i kandydata stabilizacji do krótkiego logu debug.
  private string _FormatDebugState()
  {
    var state = _store.GetDebugState();
    return $"snapshot={state.Snapshot.Status} lastSampleAt={_FormatTime(state.Snapshot.LastSampleAt)} stableSince={_FormatTime(state.Snapshot.StableSince)} candidate={state.CandidateStatus?.ToString() ?? "-"} candidateSince={_FormatTime(state.CandidateSince)}";
  }

  // Formatuje czas w sposób czytelny w konsoli.
  private static string _FormatTime(DateTimeOffset? value)
  {
    return value?.ToString("HH:mm:ss.fff") ?? "-";
  }

  // Escapuje znaki sterujące, żeby surowa linia była widoczna w jednym wpisie logu.
  private static string _EscapeForLog(string value)
  {
    var builder = new StringBuilder(value.Length);
    foreach (var character in value)
    {
      switch (character)
      {
        case '\r':
          builder.Append("\\r");
          break;
        case '\n':
          builder.Append("\\n");
          break;
        case '\t':
          builder.Append("\\t");
          break;
        default:
          if (char.IsControl(character))
            builder.Append($"\\u{(int)character:X4}");
          else
            builder.Append(character);
          break;
      }
    }

    return builder.ToString();
  }

  // Pokazuje kody znaków odebranej linii po dekodowaniu przez SerialPort.
  private static string _FormatCodeUnits(string value)
  {
    if (value.Length == 0)
      return "-";

    var builder = new StringBuilder(value.Length * 3);
    foreach (var character in value)
    {
      if (builder.Length > 0)
        builder.Append(' ');

      builder.Append(((int)character).ToString("X2"));
    }

    return builder.ToString();
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
  private readonly Action<string>? _writeLog;

  private SensorSnapshot _snapshot = new(SensorStatus.SensorNotAvailable, null, DateTimeOffset.Now);
  private SensorStatus? _candidateStatus;
  private DateTimeOffset? _candidateSince;
  private DateTimeOffset? _lastSampleAt;

  /// <summary>Tworzy magazyn stanu z opcjonalnym miejscem zapisu komunikatów statusu.</summary>
  public SensorStateStore(Action<string>? writeLog = null)
  {
    _writeLog = writeLog;
  }

  /// <summary>Pobiera aktualny snapshot stanu sensora.</summary>
  public SensorSnapshot GetSnapshot()
  {
    lock (_syncRoot)
      return _snapshot;
  }

  /// <summary>Pobiera techniczny stan stabilizatora do logowania diagnostycznego.</summary>
  public SensorDebugState GetDebugState()
  {
    lock (_syncRoot)
      return new SensorDebugState(_snapshot, _candidateStatus, _candidateSince);
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
  public void RecordSample(SensorStatus sampleStatus, SensorSampleDecision sampleDecision, TimeSpan stateChangeDelay)
  {
    SensorSnapshot? changedSnapshot = null;
    SensorSampleDecision? changedSampleDecision = null;
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
      changedSampleDecision = sampleDecision;
    }

    if (changedSnapshot is not null)
      _Publish(changedSnapshot, changedSampleDecision);
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
  private void _Publish(SensorSnapshot snapshot, SensorSampleDecision? sampleDecision = null)
  {
    foreach (var (id, writer) in _subscriptions)
    {
      if (!writer.TryWrite(snapshot))
        _subscriptions.TryRemove(id, out _);
    }

    var message = _FormatStatusLog(snapshot, sampleDecision);
    if (_writeLog is not null)
      _writeLog(message);
    else
      Console.WriteLine(message);
  }

  // Składa krótki wpis konsoli dla stabilnej zmiany statusu.
  private static string _FormatStatusLog(SensorSnapshot snapshot, SensorSampleDecision? sampleDecision)
  {
    var message = $"{snapshot.StableSince:HH:mm:ss} status={snapshot.Status}";
    if (sampleDecision is null)
      return message;

    return $"{message} present={sampleDecision.Present} targetStatus={sampleDecision.TargetStatus} distanceCm={sampleDecision.DistanceCm?.ToString() ?? "-"} maxDistanceCm={sampleDecision.MaxDistanceCm} decision={sampleDecision.Decision}";
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

internal sealed class SensorDebugLogger : IDisposable
{
  private const long MaxLogBytes = 100 * 1024;

  private readonly object _syncRoot = new();
  private readonly StreamWriter _writer;
  private long _writtenBytes;
  private bool _isDisabled;

  // Zachowuje otwarty plik logu dla jednego uruchomienia debug.
  private SensorDebugLogger(string path, StreamWriter writer)
  {
    Path = path;
    _writer = writer;
  }

  public string Path { get; }

  /// <summary>Tworzy plik diagnostyczny dla bieżącego uruchomienia hosta.</summary>
  public static SensorDebugLogger Create()
  {
    var directory = System.IO.Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "StandPulse",
      "SensorHost",
      "logs");
    Directory.CreateDirectory(directory);

    var path = System.IO.Path.Combine(directory, $"sensorhost-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.log");
    var writer = new StreamWriter(path, append: false, new UTF8Encoding(false))
    {
      AutoFlush = true
    };

    var logger = new SensorDebugLogger(path, writer);
    logger.WriteLine("Debug log started.");
    logger.WriteLine($"Debug log limit: {MaxLogBytes} bytes.");
    return logger;
  }

  /// <summary>Zapisuje jedną linię logu z lokalnym znacznikiem czasu.</summary>
  public void WriteLine(string message)
  {
    lock (_syncRoot)
    {
      if (_isDisabled)
        return;

      var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} {message}";
      var bytes = Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;

      if (_writtenBytes + bytes > MaxLogBytes)
      {
        var stopLine = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} Debug log stopped after reaching {MaxLogBytes} bytes.";
        _writer.WriteLine(stopLine);
        _writtenBytes += Encoding.UTF8.GetByteCount(stopLine) + Environment.NewLine.Length;
        _isDisabled = true;
        return;
      }

      _writer.WriteLine(line);
      _writtenBytes += bytes;
    }
  }

  /// <summary>Zamyka plik logu po zakończeniu procesu.</summary>
  public void Dispose()
  {
    lock (_syncRoot)
      _writer.Dispose();
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

internal sealed record SensorDebugState(
  SensorSnapshot Snapshot,
  SensorStatus? CandidateStatus,
  DateTimeOffset? CandidateSince);

internal sealed record SensorSampleDecision(
  bool Present,
  string TargetStatus,
  int? DistanceCm,
  int MaxDistanceCm,
  SensorStatus Decision);

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

internal sealed record SensorFirmwareEvent(string Event)
{
  /// <summary>Parsuje zdarzenie techniczne z firmware sensora.</summary>
  public static SensorFirmwareEvent? Parse(string line)
  {
    using var json = JsonDocument.Parse(line);
    var root = json.RootElement;

    if (!root.TryGetProperty("event", out var eventProperty))
      return null;

    return new SensorFirmwareEvent(eventProperty.GetString() ?? "");
  }
}

internal sealed record HostSettings(
  SensorSourceSettings Source,
  SerialSettings Serial,
  PresenceSettings Presence,
  PipeSettings Pipe,
  FakeSensorSettings Fake)
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
    new SensorSourceSettings("serial"),
    new SerialSettings("COM4", 115200),
    new PresenceSettings(120, 10, 5, 3, 20),
    new PipeSettings("StandPulse.Sensor"),
    FakeSensorSettings.Default);

  // Uzupełnia brakujące albo niepoprawne wartości konfiguracji.
  private HostSettings Normalize()
  {
    var source = Source ?? Default.Source;
    var serial = Serial ?? Default.Serial;
    var presence = Presence ?? Default.Presence;
    var pipe = Pipe ?? Default.Pipe;
    var fake = Fake ?? Default.Fake;

    if (!source.IsSerial && !source.IsFake)
      source = Default.Source;

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

    if (presence.MaxConsecutiveSensorErrors <= 0)
      presence = presence with { MaxConsecutiveSensorErrors = Default.Presence.MaxConsecutiveSensorErrors };

    if (presence.MaxStartupInvalidLines <= 0)
      presence = presence with { MaxStartupInvalidLines = Default.Presence.MaxStartupInvalidLines };

    if (string.IsNullOrWhiteSpace(pipe.Name))
      pipe = pipe with { Name = Default.Pipe.Name };

    if (fake.IntervalMilliseconds <= 0)
      fake = fake with { IntervalMilliseconds = Default.Fake.IntervalMilliseconds };

    if (fake.Samples is null || fake.Samples.Count == 0)
      fake = fake with { Samples = Default.Fake.Samples };

    return new HostSettings(source, serial, presence, pipe, fake);
  }
}

internal sealed record SensorSourceSettings(string Mode)
{
  public bool IsSerial => string.Equals(Mode, "serial", StringComparison.OrdinalIgnoreCase);
  public bool IsFake => string.Equals(Mode, "fake", StringComparison.OrdinalIgnoreCase);
}

internal sealed record SerialSettings(
  string PortName,
  int BaudRate);

internal sealed record PresenceSettings(
  int MaxDistanceCm,
  int StateChangeDelaySeconds,
  int SampleTimeoutSeconds,
  int MaxConsecutiveSensorErrors,
  int MaxStartupInvalidLines);

internal sealed record PipeSettings(string Name);

internal sealed record FakeSensorSettings(
  int IntervalMilliseconds,
  List<FakeSensorSampleSettings> Samples)
{
  public static FakeSensorSettings Default => new(
    500,
    [
      new FakeSensorSampleSettings(null, true, "StaticTarget", 64, 12, null),
      new FakeSensorSampleSettings(null, false, "NoTarget", null, 12, null)
    ]);
}

internal sealed record FakeSensorSampleSettings(
  string? Event,
  bool Present,
  string TargetStatus,
  int? DistanceCm,
  int Repeat,
  string? RawLine);
