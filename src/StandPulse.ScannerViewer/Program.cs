using System.Drawing;
using System.IO.Ports;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;

namespace StandPulse.ScannerViewer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ScannerViewerForm());
    }
}

internal sealed class ScannerViewerForm : Form
{
    private readonly ViewerSettings _settings = ViewerSettings.Load();
    private readonly ComboBox _portCombo = new();
    private readonly ComboBox _baudCombo = new();
    private readonly Button _refreshButton = new();
    private readonly Button _connectButton = new();
    private readonly Label _connectionLabel = new();
    private readonly Label _stateLabel = new();
    private readonly Label _distanceLabel = new();
    private readonly Label _targetStatusLabel = new();
    private readonly Label _lastSeenLabel = new();
    private readonly ListBox _stateLogList = new();
    private readonly System.Windows.Forms.Timer _ageTimer = new();

    private SerialPort? _serialPort;
    private DateTimeOffset? _lastSampleAt;
    private bool? _lastLoggedPresent;
    private bool? _displayedPresent;
    private bool? _candidatePresent;
    private DateTimeOffset? _candidateSince;
    private int _samplesRead;
    private int _parseErrors;

    public ScannerViewerForm()
    {
        Text = $"StandPulse Scanner v{_GetProductVersion()}";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(700, 520);
        Size = new Size(790, 600);
        Font = new Font("Segoe UI", 12F);
        BackColor = Color.FromArgb(246, 248, 250);

        BuildLayout();
        RefreshPorts();

        _ageTimer.Interval = 500;
        _ageTimer.Tick += (_, _) => UpdateSampleAge();
        _ageTimer.Start();

        FormClosing += (_, _) => Disconnect();
        Shown += (_, _) =>
        {
            if (SelectPort(_settings.Serial.PortName))
            {
                Connect();
            }
        };
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(10),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 115));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var headerPanel = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 4),
        };
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.Controls.Add(headerPanel, 0, 0);

        var header = new Label
        {
            Text = "StandPulse Scanner",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 22F, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 58, 82),
            Margin = new Padding(0),
        };
        headerPanel.Controls.Add(header, 0, 0);

        var versionLabel = new Label
        {
            Text = $"v{_GetProductVersion()}",
            AutoSize = true,
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Font = new Font(Font.FontFamily, 10F, FontStyle.Regular),
            ForeColor = Color.FromArgb(91, 101, 111),
            Margin = new Padding(12, 9, 0, 0),
        };
        headerPanel.Controls.Add(versionLabel, 1, 0);

        var toolbar = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 8),
        };
        root.Controls.Add(toolbar, 0, 1);

        _portCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _portCombo.Width = 120;
        _portCombo.Font = new Font(Font.FontFamily, 12F);
        toolbar.Controls.Add(LabeledControl("Port", _portCombo));

        _baudCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _baudCombo.Width = 120;
        _baudCombo.Font = new Font(Font.FontFamily, 12F);
        _baudCombo.Items.AddRange(["115200", "9600"]);
        var configuredBaudRate = _settings.Serial.BaudRate.ToString();
        if (!_baudCombo.Items.Contains(configuredBaudRate))
        {
            _baudCombo.Items.Add(configuredBaudRate);
        }

        _baudCombo.SelectedItem = configuredBaudRate;
        toolbar.Controls.Add(LabeledControl("Baud", _baudCombo));

        _refreshButton.Text = "Odśwież";
        _refreshButton.AutoSize = true;
        _refreshButton.Font = new Font(Font.FontFamily, 12F);
        _refreshButton.Padding = new Padding(8, 4, 8, 4);
        _refreshButton.Click += (_, _) => RefreshPorts();
        toolbar.Controls.Add(_refreshButton);

        _connectButton.Text = "Połącz";
        _connectButton.AutoSize = true;
        _connectButton.Font = new Font(Font.FontFamily, 12F);
        _connectButton.Padding = new Padding(8, 4, 8, 4);
        _connectButton.Click += (_, _) =>
        {
            if (_serialPort?.IsOpen == true)
            {
                Disconnect();
            }
            else
            {
                Connect();
            }
        };
        toolbar.Controls.Add(_connectButton);

        _connectionLabel.AutoSize = true;
        _connectionLabel.Font = new Font(Font.FontFamily, 12F, FontStyle.Bold);
        _connectionLabel.Padding = new Padding(10, 8, 10, 8);
        _connectionLabel.Margin = new Padding(10, 0, 0, 0);
        toolbar.Controls.Add(_connectionLabel);

        _stateLabel.Dock = DockStyle.Fill;
        _stateLabel.TextAlign = ContentAlignment.MiddleCenter;
        _stateLabel.Font = new Font(Font.FontFamily, 38F, FontStyle.Bold);
        _stateLabel.Margin = new Padding(0, 0, 0, 10);
        root.Controls.Add(_stateLabel, 0, 2);

        var distancePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 10),
            BackColor = Color.White,
            Padding = new Padding(14, 8, 14, 8),
        };
        distancePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        distancePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        distancePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(distancePanel, 0, 3);

        var distanceTitle = new Label
        {
            Text = "ODLEGŁOŚĆ",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font.FontFamily, 16F, FontStyle.Bold),
            ForeColor = Color.FromArgb(75, 85, 99),
            Margin = new Padding(0, 0, 0, 4),
        };
        distancePanel.Controls.Add(distanceTitle, 0, 0);

        _distanceLabel.Dock = DockStyle.Fill;
        _distanceLabel.TextAlign = ContentAlignment.MiddleCenter;
        _distanceLabel.Font = new Font(Font.FontFamily, 58F, FontStyle.Bold);
        _distanceLabel.ForeColor = Color.FromArgb(17, 24, 39);
        _distanceLabel.Margin = new Padding(0);
        _distanceLabel.AutoEllipsis = false;
        distancePanel.Controls.Add(_distanceLabel, 0, 1);

        var details = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Margin = new Padding(0),
        };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        details.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        details.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        details.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        details.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        details.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(details, 0, 4);

        AddMetric(details, 0, "Target", _targetStatusLabel);
        AddMetric(details, 1, "Ostatnia próbka", _lastSeenLabel);

        var logTitle = new Label
        {
            Text = "Historia zmian stanu",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 78, 121),
            Margin = new Padding(0, 18, 0, 8),
        };
        details.Controls.Add(logTitle, 0, 2);
        details.SetColumnSpan(logTitle, 2);

        _stateLogList.Dock = DockStyle.Fill;
        _stateLogList.Font = new Font(Font.FontFamily, 14F, FontStyle.Bold);
        _stateLogList.BackColor = Color.White;
        _stateLogList.BorderStyle = BorderStyle.FixedSingle;
        _stateLogList.IntegralHeight = false;
        details.Controls.Add(_stateLogList, 0, 3);
        details.SetColumnSpan(_stateLogList, 2);

        var note = new Label
        {
            Text = $"Stan zmienia się dopiero po {_settings.Presence.StateChangeDelaySeconds} s stabilnego sygnału. OBECNY wymaga dystansu < {_settings.Presence.MaxDistanceCm} cm.",
            AutoSize = true,
            ForeColor = Color.FromArgb(91, 101, 111),
            Margin = new Padding(0, 8, 0, 0),
        };
        details.Controls.Add(note, 0, 4);
        details.SetColumnSpan(note, 2);

        SetDisconnectedUi("Niepołączony");
    }

    private static Control LabeledControl(string label, Control control)
    {
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            Margin = new Padding(0, 0, 12, 0),
        };
        panel.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Font = new Font("Segoe UI", 11F),
            ForeColor = Color.FromArgb(91, 101, 111),
            Margin = new Padding(0, 0, 0, 3),
        });
        panel.Controls.Add(control);
        return panel;
    }

    // Pobiera wersję wpisaną do assembly podczas kompilacji.
    private static string _GetProductVersion()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        return string.IsNullOrWhiteSpace(version) ? "-" : version;
    }

    private void AddMetric(TableLayoutPanel parent, int row, string label, Label valueLabel)
    {
        var name = new Label
        {
            Text = label,
            AutoSize = true,
            Font = new Font(Font.FontFamily, 13F),
            ForeColor = Color.FromArgb(91, 101, 111),
            Margin = new Padding(0, 6, 10, 8),
        };
        parent.Controls.Add(name, 0, row);

        valueLabel.Text = "-";
        valueLabel.AutoSize = true;
        valueLabel.Font = new Font(Font.FontFamily, 14F, FontStyle.Bold);
        valueLabel.Margin = new Padding(0, 6, 0, 8);
        parent.Controls.Add(valueLabel, 1, row);
    }

    private void RefreshPorts()
    {
        var previous = _portCombo.SelectedItem as string;
        _portCombo.Items.Clear();
        _portCombo.Items.AddRange(SerialPort.GetPortNames().OrderBy(x => x).Cast<object>().ToArray());

        if (!SelectPort(previous) && !SelectPort(_settings.Serial.PortName) && _portCombo.Items.Count > 0)
        {
            _portCombo.SelectedIndex = 0;
        }
    }

    private bool SelectPort(string? portName)
    {
        if (string.IsNullOrWhiteSpace(portName))
        {
            return false;
        }

        foreach (var item in _portCombo.Items)
        {
            if (string.Equals((string)item, portName, StringComparison.OrdinalIgnoreCase))
            {
                _portCombo.SelectedItem = item;
                return true;
            }
        }

        return false;
    }

    private void Connect()
    {
        if (_portCombo.SelectedItem is not string portName)
        {
            SetDisconnectedUi("Brak portu COM");
            return;
        }

        if (!int.TryParse(_baudCombo.SelectedItem as string, out var baudRate))
        {
            baudRate = 115200;
        }

        try
        {
            Disconnect();

            _serialPort = new SerialPort(portName, baudRate)
            {
                NewLine = "\n",
                ReadTimeout = 200,
                DtrEnable = false,
                RtsEnable = false,
            };
            _serialPort.DataReceived += SerialPortOnDataReceived;
            _serialPort.Open();

            _samplesRead = 0;
            _parseErrors = 0;
            _lastSampleAt = null;
            _lastLoggedPresent = null;
            _displayedPresent = null;
            _candidatePresent = null;
            _candidateSince = null;
            _stateLogList.Items.Clear();

            _connectButton.Text = "Rozłącz";
            _connectionLabel.Text = $"Połączony: {portName} @ {baudRate}";
            _connectionLabel.BackColor = Color.FromArgb(222, 247, 226);
            _connectionLabel.ForeColor = Color.FromArgb(22, 101, 52);
            SetWaitingUi();
        }
        catch (Exception ex)
        {
            SetDisconnectedUi(ex.Message);
        }
    }

    private void Disconnect()
    {
        if (_serialPort is null)
        {
            return;
        }

        try
        {
            _serialPort.DataReceived -= SerialPortOnDataReceived;
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }
        finally
        {
            _serialPort.Dispose();
            _serialPort = null;
            _connectButton.Text = "Połącz";
        }
    }

    private void SerialPortOnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (sender is not SerialPort port)
        {
            return;
        }

        while (port.IsOpen && port.BytesToRead > 0)
        {
            try
            {
                var line = port.ReadLine().Trim();
                if (line.Length > 0)
                {
                    BeginInvoke(() => HandleLine(line));
                }
            }
            catch (TimeoutException)
            {
                return;
            }
            catch (InvalidOperationException)
            {
                return;
            }
        }
    }

    private void HandleLine(string line)
    {
        try
        {
            var sample = ScannerSample.Parse(line);
            if (sample is null)
            {
                _parseErrors++;
                return;
            }

            _samplesRead++;
            _lastSampleAt = DateTimeOffset.Now;

            _targetStatusLabel.Text = sample.TargetStatus;
            _distanceLabel.Text = sample.DistanceCm.HasValue ? $"{sample.DistanceCm.Value} cm" : "brak";

            UpdateStablePresence(IsAtDesk(sample), sample);
            UpdateSampleAge();
        }
        catch (JsonException)
        {
            _parseErrors++;
        }
    }

    private bool IsAtDesk(ScannerSample sample)
    {
        return sample.Present &&
               sample.DistanceCm.HasValue &&
               sample.DistanceCm.Value < _settings.Presence.MaxDistanceCm;
    }

    private void UpdateStablePresence(bool presentAtDesk, ScannerSample sample)
    {
        var now = DateTimeOffset.Now;

        if (_candidatePresent != presentAtDesk)
        {
            _candidatePresent = presentAtDesk;
            _candidateSince = now;

            if (_displayedPresent is null)
            {
                SetEvaluatingUi();
            }

            return;
        }

        var stableFor = now - (_candidateSince ?? now);
        if (stableFor < TimeSpan.FromSeconds(_settings.Presence.StateChangeDelaySeconds))
        {
            if (_displayedPresent is null)
            {
                SetEvaluatingUi();
            }

            return;
        }

        if (_displayedPresent == presentAtDesk)
        {
            return;
        }

        _displayedPresent = presentAtDesk;
        ApplyPresenceUi(presentAtDesk);
        AppendStateChangeIfNeeded(presentAtDesk, sample);
    }

    private void ApplyPresenceUi(bool present)
    {
        if (present)
        {
            _stateLabel.Text = "OBECNY";
            _stateLabel.BackColor = Color.FromArgb(209, 250, 229);
            _stateLabel.ForeColor = Color.FromArgb(6, 95, 70);
        }
        else
        {
            _stateLabel.Text = "NIEOBECNY";
            _stateLabel.BackColor = Color.FromArgb(229, 231, 235);
            _stateLabel.ForeColor = Color.FromArgb(55, 65, 81);
        }
    }

    private void SetEvaluatingUi()
    {
        _stateLabel.Text = "USTALAM";
        _stateLabel.BackColor = Color.FromArgb(254, 243, 199);
        _stateLabel.ForeColor = Color.FromArgb(146, 64, 14);
    }

    private void AppendStateChangeIfNeeded(bool present, ScannerSample sample)
    {
        if (_lastLoggedPresent == present)
        {
            return;
        }

        _lastLoggedPresent = present;

        var state = present ? "OBECNY" : "NIEOBECNY";
        var distance = sample.DistanceCm.HasValue ? $"{sample.DistanceCm.Value} cm" : "-";
        var entry = $"{DateTime.Now:HH:mm:ss}  {state,-10}  {distance,7}  {sample.TargetStatus}";

        _stateLogList.Items.Insert(0, entry);

        while (_stateLogList.Items.Count > 40)
        {
            _stateLogList.Items.RemoveAt(_stateLogList.Items.Count - 1);
        }
    }

    private void UpdateSampleAge()
    {
        if (_lastSampleAt is null)
        {
            _lastSeenLabel.Text = $"czekam na dane, błędy parsowania: {_parseErrors}";
            return;
        }

        var age = DateTimeOffset.Now - _lastSampleAt.Value;
        _lastSeenLabel.Text = $"{age.TotalSeconds:0.0}s temu, próbki: {_samplesRead}, błędy: {_parseErrors}";
    }

    private void SetWaitingUi()
    {
        _stateLabel.Text = "CZEKAM";
        _stateLabel.BackColor = Color.FromArgb(254, 243, 199);
        _stateLabel.ForeColor = Color.FromArgb(146, 64, 14);
        _targetStatusLabel.Text = "-";
        _distanceLabel.Text = "-";
        _displayedPresent = null;
        _candidatePresent = null;
        _candidateSince = null;
        UpdateSampleAge();
    }

    private void SetDisconnectedUi(string message)
    {
        _connectionLabel.Text = message;
        _connectionLabel.BackColor = Color.FromArgb(243, 244, 246);
        _connectionLabel.ForeColor = Color.FromArgb(75, 85, 99);
        _stateLabel.Text = "NIEPOŁĄCZONY";
        _stateLabel.BackColor = Color.FromArgb(229, 231, 235);
        _stateLabel.ForeColor = Color.FromArgb(55, 65, 81);
        _targetStatusLabel.Text = "-";
        _distanceLabel.Text = "-";
        _lastSeenLabel.Text = "-";
        _displayedPresent = null;
        _candidatePresent = null;
        _candidateSince = null;
        _connectButton.Text = "Połącz";
    }
}

internal sealed record ScannerSample(
    bool Present,
    string TargetStatus,
    int? DistanceCm)
{
    public static ScannerSample? Parse(string line)
    {
        using var json = JsonDocument.Parse(line);
        var root = json.RootElement;

        if (!root.TryGetProperty("present", out var presentProperty))
        {
            return null;
        }

        var present = presentProperty.GetBoolean();
        var targetStatus = root.TryGetProperty("targetStatus", out var targetStatusProperty)
            ? targetStatusProperty.GetString() ?? "-"
            : "-";

        int? distanceCm = null;
        if (root.TryGetProperty("distanceCm", out var distanceCmProperty) && distanceCmProperty.ValueKind == JsonValueKind.Number)
        {
            distanceCm = distanceCmProperty.GetInt32();
        }
        else if (root.TryGetProperty("distanceMm", out var legacyDistanceProperty) && legacyDistanceProperty.ValueKind == JsonValueKind.Number)
        {
            distanceCm = legacyDistanceProperty.GetInt32();
        }

        return new ScannerSample(present, targetStatus, distanceCm);
    }
}

internal sealed record ViewerSettings(
    SerialSettings Serial,
    PresenceSettings Presence)
{
    public static ViewerSettings Load()
    {
        var defaults = Default;
        var path = Path.Combine(AppContext.BaseDirectory, "sensor-settings.json");

        if (!File.Exists(path))
        {
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<ViewerSettings>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return loaded?.Normalize() ?? defaults;
        }
        catch
        {
            return defaults;
        }
    }

    private static ViewerSettings Default => new(
        new SerialSettings("COM4", 115200),
        new PresenceSettings(120, 10));

    private ViewerSettings Normalize()
    {
        var serial = Serial ?? Default.Serial;
        var presence = Presence ?? Default.Presence;

        if (string.IsNullOrWhiteSpace(serial.PortName))
        {
            serial = serial with { PortName = Default.Serial.PortName };
        }

        if (serial.BaudRate <= 0)
        {
            serial = serial with { BaudRate = Default.Serial.BaudRate };
        }

        if (presence.MaxDistanceCm <= 0)
        {
            presence = presence with { MaxDistanceCm = Default.Presence.MaxDistanceCm };
        }

        if (presence.StateChangeDelaySeconds <= 0)
        {
            presence = presence with { StateChangeDelaySeconds = Default.Presence.StateChangeDelaySeconds };
        }

        return new ViewerSettings(serial, presence);
    }
}

internal sealed record SerialSettings(
    string PortName,
    int BaudRate);

internal sealed record PresenceSettings(
    int MaxDistanceCm,
    int StateChangeDelaySeconds);
