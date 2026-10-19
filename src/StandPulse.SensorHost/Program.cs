using System.Collections.Concurrent;
using System.IO.Pipes;
using System.IO.Ports;
using System.Runtime.InteropServices;
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
    var traceEnabled = _IsTraceCommand(args);
    if (!_ShouldRun(args) && !debugEnabled && !traceEnabled)
    {
      _WriteHelp();
      return 0;
    }

    var settingsWarnings = new List<string>();
    HostSettings settings;
    try
    {
      settings = HostSettings.Load(settingsWarnings);
    }
    catch (InvalidDataException ex)
    {
      // Uszkodzona konfiguracja zatrzymuje start, bo cicha praca na wartościach domyślnych dawała fałszywe przerwy.
      _WriteStartupFailure(ex.Message, null);
      return 1;
    }

    using var instanceMutex = new Mutex(true, $"Local\\StandPulse.SensorHost.{settings.Pipe.Name}", out var isFirstInstance);
    if (!isFirstInstance)
    {
      _WriteStartupFailure($"StandPulse.SensorHost już działa dla pipe {settings.Pipe.Name}.", settings.Log.Directory);
      return 3;
    }

    SensorHostLogger logger;
    try
    {
      logger = SensorHostLogger.Create(traceEnabled, settings.Log.Directory);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
      _WriteStartupFailure($"Nie można otworzyć logu hosta: {ex.Message}", null);
      return 4;
    }

    using var ownedLogger = logger;
    Action<string> writeLog = logger.WriteLine;
    foreach (var warning in settingsWarnings)
    {
      writeLog($"Konfiguracja: {warning}");
      _WriteConsoleLog($"Konfiguracja: {warning}");
    }

    var effectors = EffectorDefinition.LoadAll(settings.Effectors.Directory, writeLog);
    using var effectorShutdownGuard = EffectorShutdownGuard.Create(effectors, writeLog);
    var store = new SensorStateStore(writeLog);
    var host = new SensorHostService(settings, effectors, store, logger);
    using var cancellation = new CancellationTokenSource();

    Console.CancelKeyPress += (_, args) =>
    {
      args.Cancel = true;
      cancellation.Cancel();
    };

    _WriteConsoleLog("StandPulse.SensorHost");
    _WriteConsoleLog($"Version: {_GetProductVersion()}");
    _WriteConsoleLog($"Mode: {(traceEnabled ? "trace" : debugEnabled ? "debug" : "run")}");
    _WriteConsoleLog($"Log: {logger.Path}");
    _WriteConsoleLog($"Source: {settings.Source.Mode}");
    if (settings.Source.IsSerial)
      _WriteConsoleLog($"Serial: {settings.Serial.PortName} @ {settings.Serial.BaudRate}");
    _WriteConsoleLog($"Pipe: {settings.Pipe.Name}");
    _WriteConsoleLog($"Effectors: {effectors.Count}");
    _WriteConsoleLog("Naciśnij Ctrl+C, aby zakończyć.");

    try
    {
      await host.RunAsync(cancellation.Token);
    }
    catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
    {
    }

    _WriteConsoleLog("Host zatrzymany.");
    return 0;
  }

  // Zgłasza przerwany start na konsoli i, jeśli się da, w logu hosta.
  private static void _WriteStartupFailure(string message, string? logDirectory)
  {
    var line = SensorHostLogger.FormatLine($"Start przerwany: {message}");
    Console.Error.WriteLine(line);

    try
    {
      var path = SensorHostLogger.GetLogPath(logDirectory);
      Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
      File.AppendAllText(path, line + Environment.NewLine, new UTF8Encoding(false));
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
      // Log bywa zajęty przez działający host; komunikat na konsoli wystarcza.
    }
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

  // Sprawdza, czy host ma zapisywac surowe probki sensora do logu.
  private static bool _IsTraceCommand(string[] args)
  {
    return args.Length == 1 && string.Equals(args[0], "trace", StringComparison.OrdinalIgnoreCase);
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

  // Wypisuje log konsolowy w takim samym formacie czasu jak plik hosta.
  private static void _WriteConsoleLog(string message)
  {
    Console.WriteLine(SensorHostLogger.FormatLine(message));
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
    Console.WriteLine("  StandPulse.SensorHost.exe trace");
    Console.WriteLine("  StandPulse.SensorHost.exe help");
    Console.WriteLine("  StandPulse.SensorHost.exe -help");
    Console.WriteLine("  StandPulse.SensorHost.exe --help");
    Console.WriteLine("  StandPulse.SensorHost.exe doc");
    Console.WriteLine();
    Console.WriteLine("Komendy:");
    Console.WriteLine("  run    Uruchamia hosta sensora z logiem operacyjnym w pliku.");
    Console.WriteLine("  debug  Działa tak samo jak run; tej nazwy używa autostart.");
    Console.WriteLine("  trace  Uruchamia hosta sensora z dodatkowym logowaniem surowych linii i probek.");
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
  private readonly IReadOnlyList<EffectorDefinition> _effectors;
  private readonly SensorStateStore _store;
  private readonly SensorHostLogger _logger;
  private int _consecutiveSensorErrors;
  private int _startupInvalidLineCount;
  private bool _receivedValidSensorMessage;

  public SensorHostService(HostSettings settings, IReadOnlyList<EffectorDefinition> effectors, SensorStateStore store, SensorHostLogger logger)
  {
    _settings = settings;
    _effectors = effectors;
    _store = store;
    _logger = logger;
  }

  /// <summary>Uruchamia odczyt sensora, kontrolę timeoutu i serwer named pipe.</summary>
  public async Task RunAsync(CancellationToken cancellationToken)
  {
    var tasks = new List<Task>
    {
      // Odczyt COM blokuje wątek na ReadLine, dlatego dostaje osobny wątek.
      _RunSupervisedAsync(
        "sensor",
        token => Task.Factory.StartNew(
          () => _ReadSamplesAsync(token),
          token,
          TaskCreationOptions.LongRunning,
          TaskScheduler.Default).Unwrap(),
        cancellationToken),
      _RunSupervisedAsync("timeout", _WatchSampleTimeoutAsync, cancellationToken),
      _RunSupervisedAsync("session", _WatchWindowsSessionAsync, cancellationToken),
      _RunSupervisedAsync("pipe", _RunPipeServerAsync, cancellationToken),
      _RunSupervisedAsync("effectors", _RunEffectorAutomationAsync, cancellationToken)
    };

    await Task.WhenAll(tasks);
  }

  // Loguje nieoczekiwany wyjątek zadania i uruchamia je ponownie, żeby host w autostarcie nie tracił funkcji po cichu.
  private async Task _RunSupervisedAsync(string name, Func<CancellationToken, Task> run, CancellationToken cancellationToken)
  {
    while (!cancellationToken.IsCancellationRequested)
    {
      try
      {
        await run(cancellationToken);
        return;
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        return;
      }
      catch (Exception ex)
      {
        _WriteOperationalLog($"Nieoczekiwany błąd zadania {name}, ponawiam: {ex}");
        await _DelayBeforeRetryAsync(cancellationToken);
      }
    }
  }

  // Uruchamia automatykę efektorów niezależnie od odczytu sensora i serwera pipe.
  private Task _RunEffectorAutomationAsync(CancellationToken cancellationToken)
  {
    var automation = new EffectorAutomationService(_effectors, _store, _WriteOperationalLog);
    return automation.RunAsync(cancellationToken);
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
        _WriteOperationalLog($"Połączono z {_settings.Serial.PortName}.");

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
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
      {
        // ArgumentException oznacza niepoprawną nazwę portu w konfiguracji.
        _WriteOperationalLog($"Sensor niedostępny: {ex.Message}");
        _store.SetTechnicalStatus(SensorStatus.SensorNotAvailable);
        await _DelayBeforeRetryAsync(cancellationToken);
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
        _store.RecordFirmwareMessage();
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
    catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
    {
      // Zły typ pola w poprawnym składniowo JSON rzuca InvalidOperationException albo FormatException.
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

  // Ustawia stan techniczny, gdy sensor przestaje wysyłać jakiekolwiek wiadomości.
  private async Task _WatchSampleTimeoutAsync(CancellationToken cancellationToken)
  {
    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
    var timeout = TimeSpan.FromSeconds(_settings.Presence.SampleTimeoutSeconds);

    while (await timer.WaitForNextTickAsync(cancellationToken))
      _store.MarkUnavailableIfSensorSilent(timeout);
  }

  // Odświeża stan sesji Windows i publikuje lock/unlock przez pipe.
  private async Task _WatchWindowsSessionAsync(CancellationToken cancellationToken)
  {
    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

    _store.RecordSessionStatus(WindowsSessionProbe.GetStatus());
    while (await timer.WaitForNextTickAsync(cancellationToken))
      _store.RecordSessionStatus(WindowsSessionProbe.GetStatus());
  }

  // Przyjmuje klientów named pipe; oczekiwanie na połączenie przerywa się przy zamykaniu hosta.
  private async Task _RunPipeServerAsync(CancellationToken cancellationToken)
  {
    while (!cancellationToken.IsCancellationRequested)
    {
      NamedPipeServerStream? pipe = null;

      try
      {
        pipe = new NamedPipeServerStream(
          _settings.Pipe.Name,
          PipeDirection.InOut,
          NamedPipeServerStream.MaxAllowedServerInstances,
          PipeTransmissionMode.Byte,
          PipeOptions.Asynchronous);

        await pipe.WaitForConnectionAsync(cancellationToken);
        var connectedPipe = pipe;
        pipe = null;
        _ = Task.Factory.StartNew(
          () => _HandlePipeClientSafely(connectedPipe, cancellationToken),
          CancellationToken.None,
          TaskCreationOptions.LongRunning,
          TaskScheduler.Default);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        pipe?.Dispose();
        return;
      }
      catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException or UnauthorizedAccessException)
      {
        pipe?.Dispose();
        _WriteOperationalLog($"Błąd serwera pipe: {ex.Message}");
        // Przerwa chroni przed gorącą pętlą, gdy pipe nie daje się utworzyć.
        await _DelayBeforeRetryAsync(cancellationToken);
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
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      // Zamykanie hosta kończy subskrypcje watch; pipe zamyka using w _HandlePipeClient.
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
    _WriteOperationalLog($"pipe command={command?.Command ?? "-"}");

    if (command?.Command == "get")
    {
      _WriteStatus(writer, _store.GetSnapshot());
      return;
    }

    if (command?.Command == "watch")
    {
      // Subskrypcja przed snapshotem, żeby zmiana między nimi nie przepadła; najwyżej przyjdzie dwa razy.
      using var subscription = _store.Subscribe();
      _WriteStatus(writer, _store.GetSnapshot());
      _WatchStatusChanges(writer, subscription, cancellationToken);
      return;
    }

    writer.WriteLine("{\"type\":\"error\",\"message\":\"Unknown command.\"}");
  }

  // Wysyła klientowi kolejne zmiany stabilnego statusu.
  private static void _WatchStatusChanges(StreamWriter writer, SensorStateStore.SensorSubscription subscription, CancellationToken cancellationToken)
  {
    while (subscription.Reader.WaitToReadAsync(cancellationToken).AsTask().GetAwaiter().GetResult())
    {
      while (subscription.Reader.TryRead(out var snapshot))
        _WriteStatus(writer, snapshot);
    }
  }

  // Serializuje snapshot do jednej linii JSON.
  private static void _WriteStatus(StreamWriter writer, SensorSnapshot snapshot)
  {
    var json = JsonSerializer.Serialize(snapshot, MessageJsonOptions);
    writer.WriteLine(json);
  }

  // Czeka przed ponowną próbą po błędzie portu COM, pipe albo zadania hosta.
  private static async Task _DelayBeforeRetryAsync(CancellationToken cancellationToken)
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
    if (_logger.TraceEnabled)
      _logger.WriteLine($"TRACE {message}");
  }

  // Kieruje zwykle komunikaty hosta do jednego logu operacyjnego i na konsole.
  private void _WriteOperationalLog(string message)
  {
    _logger.WriteLine(message);
    Console.WriteLine(SensorHostLogger.FormatLine(message));
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

internal sealed record EffectorDefinition
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
  };

  public string Name { get; init; } = "";
  public string Type { get; init; } = "";
  public bool Enabled { get; init; }
  public ShellyPlugConnectionSettings? Connection { get; init; }
  public EffectorCondition? TurnOnWhen { get; init; }
  public string DefaultState { get; init; } = "off";

  /// <summary>Wczytuje definicje efektorów z katalogu konfiguracji.</summary>
  public static IReadOnlyList<EffectorDefinition> LoadAll(string directory, Action<string> writeLog)
  {
    var resolvedDirectory = Path.IsPathRooted(directory)
      ? directory
      : Path.Combine(AppContext.BaseDirectory, directory);

    if (!Directory.Exists(resolvedDirectory))
    {
      writeLog($"Effectors: katalog nie istnieje '{resolvedDirectory}'.");
      return [];
    }

    var effectors = new List<EffectorDefinition>();
    foreach (var path in Directory.EnumerateFiles(resolvedDirectory, "*.json").Order(StringComparer.OrdinalIgnoreCase))
    {
      try
      {
        var json = File.ReadAllText(path);
        var effector = JsonSerializer.Deserialize<EffectorDefinition>(json, JsonOptions)?.Normalize(Path.GetFileNameWithoutExtension(path));
        if (effector is not null)
          effectors.Add(effector);
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
      {
        writeLog($"Effectors: nie udało się wczytać '{path}': {ex.Message}");
      }
    }

    writeLog($"Effectors: wczytano {effectors.Count} z '{resolvedDirectory}'.");
    return effectors;
  }

  /// <summary>Sprawdza, czy aktualny snapshot spełnia regułę włączenia efektora.</summary>
  public bool ShouldTurnOn(SensorSnapshot snapshot)
  {
    if (!string.Equals(DefaultState, "off", StringComparison.OrdinalIgnoreCase))
      return false;

    return TurnOnWhen?.Matches(snapshot) == true;
  }

  // Uzupełnia nazwę z pliku i odcina definicje bez typu.
  private EffectorDefinition? Normalize(string fallbackName)
  {
    if (string.IsNullOrWhiteSpace(Type))
      return null;

    return this with
    {
      Name = string.IsNullOrWhiteSpace(Name) ? fallbackName : Name,
      Connection = Connection?.Normalize(),
      DefaultState = string.IsNullOrWhiteSpace(DefaultState) ? "off" : DefaultState
    };
  }
}

internal sealed record EffectorCondition(
  SensorStatus? SensorStatus,
  WindowsSessionStatus? SessionStatus,
  bool? WorkPresence)
{
  /// <summary>Sprawdza dopasowanie warunków do snapshotu pipe.</summary>
  public bool Matches(SensorSnapshot snapshot)
  {
    if (SensorStatus.HasValue && snapshot.SensorStatus != SensorStatus.Value)
      return false;

    if (SessionStatus.HasValue && snapshot.SessionStatus != SessionStatus.Value)
      return false;

    if (WorkPresence.HasValue && snapshot.WorkPresence != WorkPresence.Value)
      return false;

    return SensorStatus.HasValue || SessionStatus.HasValue || WorkPresence.HasValue;
  }
}

internal sealed record ShellyPlugConnectionSettings(
  string Mode,
  string ServerUri,
  string DeviceId,
  string AuthKeyEnvironmentVariable,
  int Channel,
  int CommandTimeoutSeconds,
  int RetryCount,
  int StateCheckIntervalSeconds)
{
  /// <summary>Uzupełnia domyślne wartości techniczne połączenia Shelly.</summary>
  public ShellyPlugConnectionSettings Normalize()
  {
    return this with
    {
      Mode = string.IsNullOrWhiteSpace(Mode) ? "cloud" : Mode,
      AuthKeyEnvironmentVariable = string.IsNullOrWhiteSpace(AuthKeyEnvironmentVariable) ? "STANDPULSE_SHELLY_AUTH_KEY" : AuthKeyEnvironmentVariable,
      Channel = Channel < 0 ? 0 : Channel,
      CommandTimeoutSeconds = CommandTimeoutSeconds <= 0 ? 10 : CommandTimeoutSeconds,
      RetryCount = RetryCount < 0 ? 2 : RetryCount,
      StateCheckIntervalSeconds = StateCheckIntervalSeconds <= 0 ? 30 : StateCheckIntervalSeconds
    };
  }
}

internal sealed class EffectorAutomationService
{
  // Górny limit backoffu ogranicza ruch do Shelly Cloud i log przy trwałej awarii.
  private const double MaxRetryDelaySeconds = 300;

  private readonly IReadOnlyList<EffectorDefinition> _effectors;
  private readonly SensorStateStore _store;
  private readonly Action<string> _writeLog;

  /// <summary>Tworzy automatykę efektorów sterowaną snapshotem sensora i sesji Windows.</summary>
  public EffectorAutomationService(IReadOnlyList<EffectorDefinition> effectors, SensorStateStore store, Action<string> writeLog)
  {
    _effectors = effectors;
    _store = store;
    _writeLog = writeLog;
  }

  /// <summary>Uruchamia obsługę wszystkich włączonych efektorów.</summary>
  public async Task RunAsync(CancellationToken cancellationToken)
  {
    var tasks = new List<Task>();
    foreach (var effector in _effectors.Where(effector => effector.Enabled))
    {
      if (string.Equals(effector.Type, "shellyPlug", StringComparison.OrdinalIgnoreCase))
      {
        tasks.Add(_RunShellyPlugAsync(effector, cancellationToken));
        continue;
      }

      _writeLog($"Effector {effector.Name}: pominięty nieobsługiwany typ '{effector.Type}'.");
    }

    if (tasks.Count == 0)
    {
      _writeLog("Effectors: brak włączonych efektorów.");
      return;
    }

    await Task.WhenAll(tasks);
  }

  // Uruchamia pojedynczy efektor Shelly Plug.
  private async Task _RunShellyPlugAsync(EffectorDefinition effector, CancellationToken cancellationToken)
  {
    var settings = effector.Connection;
    if (settings is null)
    {
      _writeLog($"Effector {effector.Name}: brak sekcji connection.");
      return;
    }

    if (!string.Equals(settings.Mode, "cloud", StringComparison.OrdinalIgnoreCase))
    {
      _writeLog($"Effector {effector.Name}: pominięty nieobsługiwany tryb Shelly '{settings.Mode}'.");
      return;
    }

    var authKey = ShellyAuthKeyReader.GetValue(settings.AuthKeyEnvironmentVariable);
    if (string.IsNullOrWhiteSpace(authKey))
    {
      _writeLog($"Effector {effector.Name}: brak zmiennej środowiskowej {settings.AuthKeyEnvironmentVariable}.");
      return;
    }

    if (!Uri.TryCreate(settings.ServerUri, UriKind.Absolute, out var serverUri))
    {
      _writeLog($"Effector {effector.Name}: niepoprawny serverUri '{settings.ServerUri}'.");
      return;
    }

    using var httpClient = new HttpClient
    {
      BaseAddress = serverUri,
      Timeout = TimeSpan.FromSeconds(settings.CommandTimeoutSeconds)
    };
    var client = new ShellyCloudClient(effector.Name, settings, authKey, httpClient, _writeLog);
    using var subscription = _store.Subscribe();
    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

    var snapshot = _store.GetSnapshot();
    bool? lastAppliedOutput = null;
    var stateCheckInterval = TimeSpan.FromSeconds(settings.StateCheckIntervalSeconds);
    var nextStateCheckAt = DateTimeOffset.Now + stateCheckInterval;
    var failureCount = 0;
    var nextAttemptAt = DateTimeOffset.MinValue;
    bool? failedOutput = null;
    _writeLog($"Effector {effector.Name}: automatyka Shelly Plug uruchomiona.");

    try
    {
      lastAppliedOutput = await _ApplyDecisionAsync(effector, client, snapshot, lastAppliedOutput, false, cancellationToken);
      TrackFailure(DateTimeOffset.Now);

      while (await timer.WaitForNextTickAsync(cancellationToken))
      {
        while (subscription.Reader.TryRead(out var changedSnapshot))
          snapshot = changedSnapshot;

        var now = DateTimeOffset.Now;
        // Po nieudanej komendzie czekamy z ponowieniem, chyba że zmienił się oczekiwany stan gniazdka.
        if (now < nextAttemptAt && effector.ShouldTurnOn(snapshot) == failedOutput)
          continue;

        var shouldVerifyState = now >= nextStateCheckAt;
        lastAppliedOutput = await _ApplyDecisionAsync(effector, client, snapshot, lastAppliedOutput, shouldVerifyState, cancellationToken);
        TrackFailure(now);

        if (shouldVerifyState)
          nextStateCheckAt = now + stateCheckInterval;
      }
    }
    finally
    {
      if (cancellationToken.IsCancellationRequested)
        await client.SetOutputAsync(false, CancellationToken.None);
    }

    // Liczy kolejne nieudane komendy i wyznacza wykładniczy odstęp do następnej próby.
    void TrackFailure(DateTimeOffset now)
    {
      var desiredOutput = effector.ShouldTurnOn(snapshot);
      if (lastAppliedOutput == desiredOutput)
      {
        failureCount = 0;
        failedOutput = null;
        nextAttemptAt = DateTimeOffset.MinValue;
        return;
      }

      failureCount++;
      failedOutput = desiredOutput;
      var delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, failureCount), MaxRetryDelaySeconds));
      nextAttemptAt = now + delay;
      _writeLog($"Effector {effector.Name}: nieudana komenda {failureCount} raz z rzędu, kolejna próba za {delay.TotalSeconds:0} s.");
    }
  }

  // Wylicza oczekiwany stan efektora i wysyła komendę tylko, gdy jest potrzebna.
  private async Task<bool?> _ApplyDecisionAsync(
    EffectorDefinition effector,
    ShellyCloudClient client,
    SensorSnapshot snapshot,
    bool? lastAppliedOutput,
    bool verifyRealState,
    CancellationToken cancellationToken)
  {
    var desiredOutput = effector.ShouldTurnOn(snapshot);
    var reason = lastAppliedOutput is null ? "startup" : "state-change";

    if (lastAppliedOutput == desiredOutput)
    {
      if (!verifyRealState)
        return lastAppliedOutput;

      var actualOutput = await client.GetOutputAsync(cancellationToken);
      if (!actualOutput.HasValue)
        return lastAppliedOutput;

      if (actualOutput.Value == desiredOutput)
        return lastAppliedOutput;

      reason = "drift";
      _writeLog($"Effector {effector.Name}: drift sensor={snapshot.SensorStatus} session={snapshot.SessionStatus} workPresence={snapshot.WorkPresence} desired={(desiredOutput ? "on" : "off")} actual={(actualOutput.Value ? "on" : "off")}");
    }

    var succeeded = await client.SetOutputAsync(desiredOutput, cancellationToken);
    if (succeeded)
      lastAppliedOutput = desiredOutput;

    _writeLog($"Effector {effector.Name}: command type={effector.Type} sensor={snapshot.SensorStatus} session={snapshot.SessionStatus} workPresence={snapshot.WorkPresence} output={(desiredOutput ? "on" : "off")} reason={reason} success={succeeded}");
    return lastAppliedOutput;
  }
}

internal sealed class ShellyCloudClient
{
  private readonly string _effectorName;
  private readonly ShellyPlugConnectionSettings _settings;
  private readonly string _authKey;
  private readonly HttpClient _httpClient;
  private readonly Action<string> _writeLog;

  /// <summary>Tworzy klienta Shelly Cloud dla jednego urządzenia Plug S.</summary>
  public ShellyCloudClient(string effectorName, ShellyPlugConnectionSettings settings, string authKey, HttpClient httpClient, Action<string> writeLog)
  {
    _effectorName = effectorName;
    _settings = settings;
    _authKey = authKey;
    _httpClient = httpClient;
    _writeLog = writeLog;
  }

  /// <summary>Ustawia wyjście gniazdka na ON albo OFF przez Shelly Cloud API.</summary>
  public async Task<bool> SetOutputAsync(bool output, CancellationToken cancellationToken)
  {
    var turn = output ? "on" : "off";

    for (var attempt = 1; attempt <= _settings.RetryCount + 1; attempt++)
    {
      try
      {
        using var content = new FormUrlEncodedContent(
        [
          new KeyValuePair<string, string>("id", _settings.DeviceId),
          new KeyValuePair<string, string>("auth_key", _authKey),
          new KeyValuePair<string, string>("channel", _settings.Channel.ToString()),
          new KeyValuePair<string, string>("turn", turn)
        ]);

        using var response = await _httpClient.PostAsync("/device/relay/control", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
          _writeLog($"Effector {_effectorName}: Shelly Cloud HTTP {(int)response.StatusCode} przy turn={turn}, próba {attempt}.");
        }
        else
        {
          await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
          using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
          if (!_TryGetCloudError(document.RootElement, out var cloudError))
            return true;

          _writeLog($"Effector {_effectorName}: Shelly Cloud odmowa przy turn={turn}, próba {attempt}: {cloudError}");
        }
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        throw;
      }
      catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
      {
        _writeLog($"Effector {_effectorName}: Shelly Cloud błąd przy turn={turn}, próba {attempt}: {ex.Message}");
      }

      if (attempt <= _settings.RetryCount)
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
    }

    return false;
  }

  /// <summary>Odczytuje realny stan przekaźnika Shelly Cloud dla skonfigurowanego kanału.</summary>
  public async Task<bool?> GetOutputAsync(CancellationToken cancellationToken)
  {
    for (var attempt = 1; attempt <= _settings.RetryCount + 1; attempt++)
    {
      try
      {
        using var content = new FormUrlEncodedContent(
        [
          new KeyValuePair<string, string>("id", _settings.DeviceId),
          new KeyValuePair<string, string>("auth_key", _authKey)
        ]);

        using var response = await _httpClient.PostAsync("/device/status", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
          _writeLog($"Effector {_effectorName}: Shelly Cloud HTTP {(int)response.StatusCode} przy status, próba {attempt}.");
        }
        else
        {
          await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
          using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
          if (_TryGetCloudError(document.RootElement, out var cloudError))
            _writeLog($"Effector {_effectorName}: Shelly Cloud odmowa przy status, próba {attempt}: {cloudError}");
          else if (_TryReadOutput(document.RootElement, out var output))
            return output;
          else
            _writeLog($"Effector {_effectorName}: Shelly Cloud brak switch:{_settings.Channel}.output w statusie, próba {attempt}.");
        }
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        throw;
      }
      catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
      {
        _writeLog($"Effector {_effectorName}: Shelly Cloud błąd przy status, próba {attempt}: {ex.Message}");
      }

      if (attempt <= _settings.RetryCount)
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
    }

    return null;
  }

  // Shelly Cloud zgłasza błędy logiczne (np. urządzenie offline) przez HTTP 200 z isok=false.
  private static bool _TryGetCloudError(JsonElement root, out string error)
  {
    error = "";
    if (root.ValueKind != JsonValueKind.Object ||
        !root.TryGetProperty("isok", out var isOk) ||
        isOk.ValueKind != JsonValueKind.False)
      return false;

    error = root.TryGetProperty("errors", out var errors) ? errors.GetRawText() : "isok=false";
    return true;
  }

  // Wyciąga output z odpowiedzi Shelly Cloud device/status.
  private bool _TryReadOutput(JsonElement root, out bool output)
  {
    output = false;
    var switchName = $"switch:{_settings.Channel}";

    if (!root.TryGetProperty("data", out var data) ||
        !data.TryGetProperty("device_status", out var deviceStatus) ||
        !deviceStatus.TryGetProperty(switchName, out var switchStatus) ||
        !switchStatus.TryGetProperty("output", out var outputElement) ||
        outputElement.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
      return false;

    output = outputElement.GetBoolean();
    return true;
  }
}

internal static class ShellyAuthKeyReader
{
  /// <summary>Pobiera klucz Shelly z procesu, profilu uzytkownika albo konfiguracji maszyny.</summary>
  public static string? GetValue(string variableName)
  {
    var processValue = Environment.GetEnvironmentVariable(variableName);
    if (!string.IsNullOrWhiteSpace(processValue))
      return processValue;

    var userValue = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.User);
    if (!string.IsNullOrWhiteSpace(userValue))
      return userValue;

    var machineValue = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Machine);
    return string.IsNullOrWhiteSpace(machineValue) ? null : machineValue;
  }
}

internal sealed class EffectorShutdownGuard : IDisposable
{
  private readonly IReadOnlyList<ShellyCloudClient> _clients;
  private readonly Action<string> _writeLog;
  private bool _isDisposed;

  // Przechowuje klientów używanych tylko podczas kończenia procesu.
  private EffectorShutdownGuard(IReadOnlyList<ShellyCloudClient> clients, Action<string> writeLog)
  {
    _clients = clients;
    _writeLog = writeLog;
    AppDomain.CurrentDomain.ProcessExit += _HandleProcessExit;
  }

  /// <summary>Tworzy strażnika, który przy kończeniu procesu próbuje wyłączyć efektory.</summary>
  public static EffectorShutdownGuard? Create(IReadOnlyList<EffectorDefinition> effectors, Action<string> writeLog)
  {
    var clients = new List<ShellyCloudClient>();
    foreach (var effector in effectors.Where(effector => effector.Enabled && string.Equals(effector.Type, "shellyPlug", StringComparison.OrdinalIgnoreCase)))
    {
      var settings = effector.Connection;
      if (settings is null || !string.Equals(settings.Mode, "cloud", StringComparison.OrdinalIgnoreCase))
        continue;

      var authKey = ShellyAuthKeyReader.GetValue(settings.AuthKeyEnvironmentVariable);
      if (string.IsNullOrWhiteSpace(authKey))
        continue;

      if (!Uri.TryCreate(settings.ServerUri, UriKind.Absolute, out var serverUri))
        continue;

      var httpClient = new HttpClient
      {
        BaseAddress = serverUri,
        Timeout = TimeSpan.FromSeconds(settings.CommandTimeoutSeconds)
      };
      clients.Add(new ShellyCloudClient(effector.Name, settings, authKey, httpClient, writeLog));
    }

    if (clients.Count == 0)
      return null;

    return new EffectorShutdownGuard(clients, writeLog);
  }

  /// <summary>Odpina handler kończenia procesu.</summary>
  public void Dispose()
  {
    if (_isDisposed)
      return;

    AppDomain.CurrentDomain.ProcessExit -= _HandleProcessExit;
    _isDisposed = true;
  }

  // Wysyła bezpieczne OFF podczas wylogowania, restartu albo zamykania procesu.
  private void _HandleProcessExit(object? sender, EventArgs args)
  {
    foreach (var client in _clients)
    {
      try
      {
        client.SetOutputAsync(false, CancellationToken.None).GetAwaiter().GetResult();
      }
      catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
      {
        _writeLog($"Effectors: nie udało się wyłączyć przy kończeniu procesu: {ex.Message}");
      }
    }
  }
}

internal static class WindowsSessionProbe
{
  private static readonly IntPtr WtsCurrentServerHandle = IntPtr.Zero;

  private const uint WtsCurrentSession = 0xFFFFFFFF;
  private const int WtsSessionInfoEx = 25;
  private const int SessionFlagsOffset = 16;
  private const int WtsSessionStateLock = 0;
  private const int WtsSessionStateUnlock = 1;

  [DllImport("wtsapi32.dll", SetLastError = true)]
  private static extern bool WTSQuerySessionInformation(
    IntPtr hServer,
    uint sessionId,
    int wtsInfoClass,
    out IntPtr ppBuffer,
    out uint pBytesReturned);

  [DllImport("wtsapi32.dll")]
  private static extern void WTSFreeMemory(IntPtr pointer);

  /// <summary>Sprawdza, czy bieżąca sesja Windows jest odblokowana i nadaje się do pracy użytkownika.</summary>
  public static WindowsSessionStatus GetStatus()
  {
    if (!WTSQuerySessionInformation(WtsCurrentServerHandle, WtsCurrentSession, WtsSessionInfoEx, out var buffer, out _))
      return WindowsSessionStatus.Unknown;

    try
    {
      var flags = Marshal.ReadInt32(buffer, SessionFlagsOffset);
      return flags switch
      {
        WtsSessionStateUnlock => WindowsSessionStatus.Active,
        WtsSessionStateLock => WindowsSessionStatus.Locked,
        _ => WindowsSessionStatus.Unknown
      };
    }
    finally
    {
      WTSFreeMemory(buffer);
    }
  }
}

internal sealed class SensorStateStore
{
  private readonly object _syncRoot = new();
  private readonly ConcurrentDictionary<Guid, ChannelWriter<SensorSnapshot>> _subscriptions = new();
  private readonly Action<string>? _writeLog;

  private SensorSnapshot _snapshot = new(
    SensorStatus.SensorNotAvailable,
    WindowsSessionStatus.Unknown,
    null,
    DateTimeOffset.Now,
    DateTimeOffset.Now);
  private SensorStatus? _candidateStatus;
  private DateTimeOffset? _candidateSince;
  private DateTimeOffset? _lastSampleAt;
  // Czas ostatniej dowolnej wiadomości z firmware; zdarzenia błędu też dowodzą, że łącze żyje.
  private DateTimeOffset? _lastMessageAt;

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
    string? transitionLog = null;
    var now = DateTimeOffset.Now;

    lock (_syncRoot)
    {
      _lastSampleAt = now;
      _lastMessageAt = now;
      // Świeżość próbki jest widoczna w get od razu, bez publikowania zdarzenia watch.
      _snapshot = _snapshot with { LastSampleAt = now };

      if (_candidateStatus != sampleStatus)
      {
        var previousCandidate = _candidateStatus;
        var previousCandidateSince = _candidateSince;
        var stableStatus = _snapshot.Status;
        _candidateStatus = sampleStatus;
        _candidateSince = now;

        if (previousCandidate.HasValue &&
            previousCandidate.Value != stableStatus &&
            sampleStatus == stableStatus)
        {
          transitionLog = _FormatCandidateCancelLog(
            previousCandidate.Value,
            stableStatus,
            now - (previousCandidateSince ?? now),
            sampleDecision);
        }
        else if (sampleStatus != stableStatus)
        {
          transitionLog = _FormatCandidateStartLog(stableStatus, sampleStatus, sampleDecision);
        }
      }
      else
      {
        var stableFor = now - (_candidateSince ?? now);
        if (stableFor >= stateChangeDelay && _snapshot.Status != sampleStatus)
        {
          var previousStatus = _snapshot.Status;
          var previousWorkPresence = _snapshot.WorkPresence;
          var nextSnapshot = _snapshot with
          {
            Status = sampleStatus,
            StableSince = _candidateSince ?? now
          };
          if (nextSnapshot.WorkPresence != previousWorkPresence)
            nextSnapshot = nextSnapshot with { WorkPresenceSince = _candidateSince ?? now };

          _snapshot = nextSnapshot;
          changedSnapshot = _snapshot;
          changedSampleDecision = sampleDecision;
          transitionLog = _FormatStateChangeLog(previousStatus, sampleStatus, stableFor, sampleDecision);
        }
      }
    }

    if (transitionLog is not null)
      _writeLog?.Invoke(transitionLog);

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

      var previousWorkPresence = _snapshot.WorkPresence;
      var nextSnapshot = _snapshot with
      {
        Status = status,
        LastSampleAt = _lastSampleAt,
        StableSince = now
      };
      if (nextSnapshot.WorkPresence != previousWorkPresence)
        nextSnapshot = nextSnapshot with { WorkPresenceSince = now };

      _snapshot = nextSnapshot;
      changedSnapshot = _snapshot;
    }

    if (changedSnapshot is not null)
      _Publish(changedSnapshot);
  }

  /// <summary>Aktualizuje stan sesji Windows publikowany razem ze statusem sensora.</summary>
  public void RecordSessionStatus(WindowsSessionStatus status)
  {
    SensorSnapshot? changedSnapshot = null;

    lock (_syncRoot)
    {
      if (_snapshot.SessionStatus == status)
        return;

      var previousWorkPresence = _snapshot.WorkPresence;
      var nextSnapshot = _snapshot with { SessionStatus = status };
      if (nextSnapshot.WorkPresence != previousWorkPresence)
        nextSnapshot = nextSnapshot with { WorkPresenceSince = DateTimeOffset.Now };

      _snapshot = nextSnapshot;
      changedSnapshot = _snapshot;
    }

    if (changedSnapshot is not null)
      _Publish(changedSnapshot, null, $"session change {_FormatStatusLog(changedSnapshot, null)}");
  }

  /// <summary>Rejestruje zdarzenie techniczne firmware jako dowód, że łącze z sensorem żyje.</summary>
  public void RecordFirmwareMessage()
  {
    lock (_syncRoot)
      _lastMessageAt = DateTimeOffset.Now;
  }

  /// <summary>Oznacza sensor jako niedostępny, jeśli od ostatniej wiadomości firmware minął timeout; trwałe błędy radaru zostają jako error.</summary>
  public void MarkUnavailableIfSensorSilent(TimeSpan timeout)
  {
    DateTimeOffset? lastMessageAt;

    lock (_syncRoot)
      lastMessageAt = _lastMessageAt;

    if (lastMessageAt is null)
      return;

    if (DateTimeOffset.Now - lastMessageAt.Value > timeout)
      SetTechnicalStatus(SensorStatus.SensorNotAvailable);
  }

  // Usuwa subskrypcję po zamknięciu połączenia klienta.
  private void _Unsubscribe(Guid id)
  {
    _subscriptions.TryRemove(id, out _);
  }

  // Rozsyła snapshot do aktywnych subskrybentów.
  private void _Publish(SensorSnapshot snapshot, SensorSampleDecision? sampleDecision = null, string? message = null)
  {
    foreach (var (id, writer) in _subscriptions)
    {
      if (!writer.TryWrite(snapshot))
        _subscriptions.TryRemove(id, out _);
    }

    message ??= _FormatStatusLog(snapshot, sampleDecision);
    if (_writeLog is not null)
      _writeLog(message);
    else
      Console.WriteLine(SensorHostLogger.FormatLine(message));
  }

  // Składa krótki wpis konsoli dla stabilnej zmiany statusu.
  private static string _FormatStatusLog(SensorSnapshot snapshot, SensorSampleDecision? sampleDecision)
  {
    var message = $"sensorStatus={snapshot.Status} sessionStatus={snapshot.SessionStatus} workPresence={snapshot.WorkPresence} lastSampleAt={_FormatTimestamp(snapshot.LastSampleAt)} stableSince={snapshot.StableSince:O} workPresenceSince={snapshot.WorkPresenceSince:O}";
    if (sampleDecision is null)
      return message;

    return $"{message} {_FormatSampleDecision(sampleDecision)}";
  }

  // Formatuje opcjonalny znacznik czasu do logu zmiany stanu.
  private static string _FormatTimestamp(DateTimeOffset? value)
  {
    return value.HasValue ? value.Value.ToString("O") : "-";
  }

  // Składa log rozpoczęcia kandydata zmiany stabilnego stanu.
  private static string _FormatCandidateStartLog(
    SensorStatus from,
    SensorStatus to,
    SensorSampleDecision sampleDecision)
  {
    return $"candidate start from={from} to={to} {_FormatSampleDecision(sampleDecision)}";
  }

  // Składa log anulowania kandydata, gdy próbki wracają do stabilnego stanu.
  private static string _FormatCandidateCancelLog(
    SensorStatus canceled,
    SensorStatus backTo,
    TimeSpan after,
    SensorSampleDecision sampleDecision)
  {
    return $"candidate cancel canceled={canceled} backTo={backTo} after={_FormatSeconds(after)}s {_FormatSampleDecision(sampleDecision)}";
  }

  // Składa log zatwierdzonej zmiany stabilnego stanu.
  private static string _FormatStateChangeLog(
    SensorStatus from,
    SensorStatus to,
    TimeSpan stableFor,
    SensorSampleDecision sampleDecision)
  {
    return $"state change from={from} to={to} stableFor={_FormatSeconds(stableFor)}s {_FormatSampleDecision(sampleDecision)}";
  }

  // Składa szczegóły próbki istotne przy diagnozowaniu fałszywej nieobecności.
  private static string _FormatSampleDecision(SensorSampleDecision sampleDecision)
  {
    return $"present={sampleDecision.Present} targetStatus={sampleDecision.TargetStatus} distanceCm={sampleDecision.DistanceCm?.ToString() ?? "-"} maxDistanceCm={sampleDecision.MaxDistanceCm} decision={sampleDecision.Decision}";
  }

  // Formatuje czas trwania z kropką dziesiętną niezależnie od ustawień Windows.
  private static string _FormatSeconds(TimeSpan value)
  {
    return value.TotalSeconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
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

internal sealed class SensorHostLogger : IDisposable
{
  // Limit chroni dysk przed logiem autostartu rosnącym bez końca przy awariach.
  private const long MaxLogBytes = 5 * 1024 * 1024;
  private const string LogFileName = "sensorhost.log";

  private readonly object _syncRoot = new();
  private StreamWriter _writer;

  // Zachowuje otwarty plik logu operacyjnego dla procesu hosta.
  private SensorHostLogger(string path, StreamWriter writer, bool traceEnabled)
  {
    Path = path;
    _writer = writer;
    TraceEnabled = traceEnabled;
  }

  public string Path { get; }

  public bool TraceEnabled { get; }

  /// <summary>Tworzy albo otwiera pojedynczy log operacyjny hosta w katalogu z konfiguracji albo domyślnym.</summary>
  public static SensorHostLogger Create(bool traceEnabled, string? directory)
  {
    var path = GetLogPath(directory);
    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
    _ArchiveIfTooLarge(path);

    var logger = new SensorHostLogger(path, _OpenWriter(path), traceEnabled);
    logger.WriteLine($"Host log started trace={traceEnabled}.");
    return logger;
  }

  /// <summary>Zwraca ścieżkę logu dla katalogu z konfiguracji; pusty katalog oznacza %LOCALAPPDATA%.</summary>
  public static string GetLogPath(string? directory)
  {
    var resolvedDirectory = string.IsNullOrWhiteSpace(directory)
      ? System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StandPulse",
        "SensorHost",
        "logs")
      : System.IO.Path.IsPathRooted(directory)
        ? directory
        : System.IO.Path.Combine(AppContext.BaseDirectory, directory);

    return System.IO.Path.Combine(resolvedDirectory, LogFileName);
  }

  /// <summary>Zapisuje jedną linię logu z lokalnym znacznikiem czasu.</summary>
  public void WriteLine(string message)
  {
    lock (_syncRoot)
    {
      _writer.WriteLine(FormatLine(message));
      if (_writer.BaseStream.Length > MaxLogBytes)
      {
        _writer.Dispose();
        _ArchiveIfTooLarge(Path);
        _writer = _OpenWriter(Path);
      }
    }
  }

  // Otwiera log w trybie dopisywania z natychmiastowym zapisem na dysk.
  private static StreamWriter _OpenWriter(string path)
  {
    return new StreamWriter(path, append: true, new UTF8Encoding(false))
    {
      AutoFlush = true
    };
  }

  // Przenosi zbyt duży log do jednej kopii .1, żeby zachować ostatnią historię.
  private static void _ArchiveIfTooLarge(string path)
  {
    try
    {
      var file = new FileInfo(path);
      if (file.Exists && file.Length > MaxLogBytes)
        File.Move(path, path + ".1", overwrite: true);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
      // Zablokowana kopia .1 nie może zatrzymać hosta; log pisze dalej do bieżącego pliku.
    }
  }

  /// <summary>Formatuje linię logu hosta z krótką sygnaturą czasu.</summary>
  public static string FormatLine(string message)
  {
    return $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} {message}";
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
  [property: JsonPropertyOrder(3)]
  WindowsSessionStatus SessionStatus,
  [property: JsonPropertyOrder(5)]
  DateTimeOffset? LastSampleAt,
  [property: JsonPropertyOrder(6)]
  DateTimeOffset StableSince,
  [property: JsonPropertyOrder(7)]
  DateTimeOffset WorkPresenceSince)
{
  [JsonPropertyOrder(0)]
  public string Type => "status";

  [JsonPropertyOrder(2)]
  public SensorStatus SensorStatus => Status;

  [JsonPropertyOrder(4)]
  public bool WorkPresence => Status == SensorStatus.Presence && SessionStatus == WindowsSessionStatus.Active;
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

internal enum WindowsSessionStatus
{
  Active,
  Locked,
  Unknown
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
  FakeSensorSettings Fake,
  EffectorsSettings Effectors,
  LogSettings Log)
{
  /// <summary>Wczytuje konfigurację hosta z pliku sensor-settings.json; uszkodzony plik zgłasza InvalidDataException, a poprawione wartości trafiają do warnings.</summary>
  public static HostSettings Load(List<string> warnings)
  {
    var path = Path.Combine(AppContext.BaseDirectory, "sensor-settings.json");

    if (!File.Exists(path))
    {
      warnings.Add($"brak pliku '{path}', używam ustawień domyślnych.");
      return Default;
    }

    HostSettings? loaded;
    try
    {
      var json = File.ReadAllText(path);
      loaded = JsonSerializer.Deserialize<HostSettings>(
        json,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
    {
      throw new InvalidDataException($"Nie udało się wczytać '{path}': {ex.Message}", ex);
    }

    if (loaded is null)
      throw new InvalidDataException($"Plik '{path}' nie zawiera ustawień.");

    return loaded.Normalize(warnings);
  }

  // Wartości domyślne odpowiadają dostarczonemu sensor-settings.json, żeby brak sekcji nie zmieniał zachowania.
  private static HostSettings Default => new(
    new SensorSourceSettings("serial"),
    new SerialSettings("COM4", 115200),
    new PresenceSettings(200, 10, 5, 3, 20),
    new PipeSettings("StandPulse.Sensor"),
    FakeSensorSettings.Default,
    new EffectorsSettings("effectors"),
    new LogSettings(null));

  // Uzupełnia brakujące sekcje i zastępuje niepoprawne wartości, zapisując ostrzeżenie dla każdej zamiany.
  private HostSettings Normalize(List<string> warnings)
  {
    var source = Source ?? Default.Source;
    var serial = Serial ?? Default.Serial;
    var presence = Presence ?? Default.Presence;
    var pipe = Pipe ?? Default.Pipe;
    var fake = Fake ?? Default.Fake;
    var effectors = Effectors ?? Default.Effectors;
    var log = Log ?? Default.Log;

    void Warn(string name, object? value, object fallback) =>
      warnings.Add($"niepoprawne {name}='{value}', używam '{fallback}'.");

    if (!source.IsSerial && !source.IsFake)
    {
      Warn("source.mode", source.Mode, Default.Source.Mode);
      source = Default.Source;
    }

    if (string.IsNullOrWhiteSpace(serial.PortName))
    {
      Warn("serial.portName", serial.PortName, Default.Serial.PortName);
      serial = serial with { PortName = Default.Serial.PortName };
    }

    if (serial.BaudRate <= 0)
    {
      Warn("serial.baudRate", serial.BaudRate, Default.Serial.BaudRate);
      serial = serial with { BaudRate = Default.Serial.BaudRate };
    }

    if (presence.MaxDistanceCm <= 0)
    {
      Warn("presence.maxDistanceCm", presence.MaxDistanceCm, Default.Presence.MaxDistanceCm);
      presence = presence with { MaxDistanceCm = Default.Presence.MaxDistanceCm };
    }

    if (presence.StateChangeDelaySeconds <= 0)
    {
      Warn("presence.stateChangeDelaySeconds", presence.StateChangeDelaySeconds, Default.Presence.StateChangeDelaySeconds);
      presence = presence with { StateChangeDelaySeconds = Default.Presence.StateChangeDelaySeconds };
    }

    if (presence.SampleTimeoutSeconds <= 0)
    {
      Warn("presence.sampleTimeoutSeconds", presence.SampleTimeoutSeconds, Default.Presence.SampleTimeoutSeconds);
      presence = presence with { SampleTimeoutSeconds = Default.Presence.SampleTimeoutSeconds };
    }

    if (presence.MaxConsecutiveSensorErrors <= 0)
    {
      Warn("presence.maxConsecutiveSensorErrors", presence.MaxConsecutiveSensorErrors, Default.Presence.MaxConsecutiveSensorErrors);
      presence = presence with { MaxConsecutiveSensorErrors = Default.Presence.MaxConsecutiveSensorErrors };
    }

    if (presence.MaxStartupInvalidLines <= 0)
    {
      Warn("presence.maxStartupInvalidLines", presence.MaxStartupInvalidLines, Default.Presence.MaxStartupInvalidLines);
      presence = presence with { MaxStartupInvalidLines = Default.Presence.MaxStartupInvalidLines };
    }

    if (string.IsNullOrWhiteSpace(pipe.Name))
    {
      Warn("pipe.name", pipe.Name, Default.Pipe.Name);
      pipe = pipe with { Name = Default.Pipe.Name };
    }

    if (fake.IntervalMilliseconds <= 0)
    {
      Warn("fake.intervalMilliseconds", fake.IntervalMilliseconds, Default.Fake.IntervalMilliseconds);
      fake = fake with { IntervalMilliseconds = Default.Fake.IntervalMilliseconds };
    }

    if (fake.Samples is null || fake.Samples.Count == 0)
      fake = fake with { Samples = Default.Fake.Samples };

    if (string.IsNullOrWhiteSpace(effectors.Directory))
      effectors = effectors with { Directory = Default.Effectors.Directory };

    return new HostSettings(source, serial, presence, pipe, fake, effectors, log);
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

internal sealed record EffectorsSettings(string Directory);

// Pusty katalog oznacza domyślny log w %LOCALAPPDATA%; własny katalog pozwala odseparować np. testy fake.
internal sealed record LogSettings(string? Directory);

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
