using System;
using System.Drawing;
using System.Diagnostics;
using System.Net;
using System.Windows.Forms;
using HslCommunication.Profinet.Melsec;

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HslCommunication;
namespace PLCMonitorSLMP
{
    public partial class Form1 : Form
    {
        private readonly ComboBox _cameraType = new ComboBox();
        private readonly TextBox _address = new TextBox();
        private readonly TextBox _port = new TextBox();
        private readonly CheckBox _isServer = new CheckBox();
        private readonly CheckBox _multi = new CheckBox();
        private readonly ListBox _log = new ListBox();
        private readonly RichTextBox _locationView = new RichTextBox();
        private readonly Dictionary<string, Button> _bitStatusButtons = new Dictionary<string, Button>();
        private readonly Label _xRegisterValue = new Label();
        private readonly Label _yRegisterValue = new Label();
        private readonly Label _rzRegisterValue = new Label();
        private readonly System.Windows.Forms.Timer _registerStatusTimer = new System.Windows.Forms.Timer();
        private readonly List<string> _locationDisplayLines = new List<string>();
        private readonly Label _status = new Label();
        private MelsecMcServer _plcServer;
        private CancellationTokenSource _runCancellation;
        private bool _resetPulseRunning;
        private readonly List<int[]> _locations = new List<int[]>();

        public Form1()
        {
            InitializeComponent();
            BuildInterface();
        }

        private void BuildInterface()
        {
            Text = "RoboLab";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1000, 680);
            MinimumSize = new Size(920, 620);
            BackColor = Color.FromArgb(240, 240, 240);
            Font = new Font("Segoe UI", 9F);

            var connect = new Button
            {
                Text = "Connect",
                Location = new Point(24, 28),
                Size = new Size(125, 38)
            };
            connect.Click += ConnectClick;

            _multi.Text = "Multi";
            _multi.Location = new Point(510, 82);
            _multi.AutoSize = true;

            var run = new Button
            {
                Text = "Run",
                Location = new Point(24, 74),
                Size = new Size(125, 38)
            };
            run.Click += RunClick;

            var reset = new Button
            {
                Text = "Reset",
                Location = new Point(24, 120),
                Size = new Size(125, 38)
            };
            reset.Click += ResetClick;

            var locationLabel = new Label
            {
                Text = "logRBVS.ini (X/Y/RZ x10000):",
                Location = new Point(24, 246),
                AutoSize = true
            };

            var tt = new Button
            {
                Text = "TT",
                Location = new Point(24, 166),
                Size = new Size(58, 32)
            };
            tt.Click += (sender, args) => AddLog("TT");

            var xt = new Button
            {
                Text = "XT",
                Location = new Point(91, 166),
                Size = new Size(58, 32)
            };
            xt.Click += (sender, args) => AddLog("XT");

            var cameraLabel = new Label
            {
                Text = "Type Camera:",
                Location = new Point(184, 40),
                AutoSize = true
            };

            _cameraType.Location = new Point(290, 31);
            _cameraType.Size = new Size(205, 28);
            _cameraType.DropDownStyle = ComboBoxStyle.DropDownList;

            var coordinatesLabel = new Label
            {
                Text = "X,Y,T:",
                Location = new Point(510, 40),
                AutoSize = true
            };

            var addressLabel = new Label
            {
                Text = "Address:",
                Location = new Point(184, 86),
                AutoSize = true
            };

            _address.Location = new Point(290, 77);
            _address.Size = new Size(205, 27);
            _address.Text = "127.0.0.1";

            _isServer.Text = "Server";
            _isServer.Location = new Point(580, 82);
            _isServer.AutoSize = true;
            _isServer.Checked = true;

            var portLabel = new Label
            {
                Text = "Port:",
                Location = new Point(184, 132),
                AutoSize = true
            };

            _port.Location = new Point(290, 123);
            _port.Size = new Size(205, 27);
            _port.Text = "4500";

            var registerPanel = new GroupBox
            {
                Text = "PLC register status",
                Location = new Point(640, 17),
                Size = new Size(340, 225),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            registerPanel.Controls.Add(CreateBitStatusButton("D0.2", new Point(14, 35)));
            registerPanel.Controls.Add(CreateBitStatusButton("D0.6", new Point(176, 35)));
            registerPanel.Controls.Add(CreateBitStatusButton("D20.1", new Point(14, 68)));
            registerPanel.Controls.Add(CreateBitStatusButton("D20.6", new Point(176, 68)));
            registerPanel.Controls.Add(CreateBitStatusButton("D20.7", new Point(14, 101)));
            registerPanel.Controls.Add(CreateBitStatusButton("D20.8", new Point(176, 101)));

            var xRegisterLabel = new Label { Text = "X D22-D23:", Location = new Point(16, 143), AutoSize = true };
            var yRegisterLabel = new Label { Text = "Y D24-D25:", Location = new Point(16, 165), AutoSize = true };
            var rzRegisterLabel = new Label { Text = "RZ D26-D27:", Location = new Point(16, 187), AutoSize = true };
            _xRegisterValue.Location = new Point(112, 143);
            _yRegisterValue.Location = new Point(112, 165);
            _rzRegisterValue.Location = new Point(112, 187);
            _xRegisterValue.AutoSize = _yRegisterValue.AutoSize = _rzRegisterValue.AutoSize = true;
            _xRegisterValue.Text = _yRegisterValue.Text = _rzRegisterValue.Text = "0";
            registerPanel.Controls.Add(xRegisterLabel);
            registerPanel.Controls.Add(yRegisterLabel);
            registerPanel.Controls.Add(rzRegisterLabel);
            registerPanel.Controls.Add(_xRegisterValue);
            registerPanel.Controls.Add(_yRegisterValue);
            registerPanel.Controls.Add(_rzRegisterValue);

            var logLabel = new Label
            {
                Text = "Activity log:",
                Location = new Point(24, 473),
                AutoSize = true
            };

            _locationView.Location = new Point(24, 269);
            _locationView.Size = new Size(952, 194);
            _locationView.ReadOnly = true;
            _locationView.WordWrap = false;
            _locationView.BackColor = Color.White;
            _locationView.Font = new Font("Consolas", 9F);

            _log.Location = new Point(24, 496);
            _log.Size = new Size(952, 128);
            _log.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                          AnchorStyles.Left | AnchorStyles.Right;

            _status.Text = "Disconnected";
            _status.Location = new Point(24, 640);
            _status.Size = new Size(952, 30);
            _status.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            _status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left |
                             AnchorStyles.Right;

            _registerStatusTimer.Interval = 250;
            _registerStatusTimer.Tick += (sender, args) => UpdateRegisterStatus();

            Controls.Add(connect);
            Controls.Add(_multi);
            Controls.Add(run);
            Controls.Add(reset);
            Controls.Add(locationLabel);
            Controls.Add(_locationView);
            Controls.Add(tt);
            Controls.Add(xt);
            Controls.Add(cameraLabel);
            Controls.Add(_cameraType);
            Controls.Add(coordinatesLabel);
            Controls.Add(addressLabel);
            Controls.Add(_address);
            Controls.Add(_isServer);
            Controls.Add(portLabel);
            Controls.Add(_port);
            Controls.Add(registerPanel);
            Controls.Add(logLabel);
            Controls.Add(_log);
            Controls.Add(_status);
            FormClosing += Form1Closing;
        }

        private Button CreateBitStatusButton(string address, Point location)
        {
            var button = new Button
            {
                Text = address + "  OFF",
                Location = location,
                Size = new Size(145, 27),
                BackColor = Color.FromArgb(125, 125, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                TabStop = false
            };
            button.FlatAppearance.BorderSize = 0;
            _bitStatusButtons.Add(address, button);
            return button;
        }

        private void UpdateRegisterStatus()
        {
            if (_plcServer == null)
            {
                foreach (KeyValuePair<string, Button> item in _bitStatusButtons)
                {
                    SetBitStatus(item.Key, false);
                }

                _xRegisterValue.Text = _yRegisterValue.Text = _rzRegisterValue.Text = "0";
                return;
            }

            foreach (KeyValuePair<string, Button> item in _bitStatusButtons)
            {
                OperateResult<bool[]> result = _plcServer.ReadBool(item.Key, 1);
                bool isOn = result.IsSuccess && result.Content != null &&
                            result.Content.Length > 0 && result.Content[0];
                SetBitStatus(item.Key, isOn);
            }

            try
            {
                _xRegisterValue.Text = ReadDwordValue("D22").ToString(CultureInfo.InvariantCulture);
                _yRegisterValue.Text = ReadDwordValue("D24").ToString(CultureInfo.InvariantCulture);
                _rzRegisterValue.Text = ReadDwordValue("D26").ToString(CultureInfo.InvariantCulture);
            }
            catch
            {
                _xRegisterValue.Text = _yRegisterValue.Text = _rzRegisterValue.Text = "Read error";
            }
        }

        private void SetBitStatus(string address, bool isOn)
        {
            Button button;
            if (!_bitStatusButtons.TryGetValue(address, out button))
            {
                return;
            }

            button.Text = address + (isOn ? "  ON" : "  OFF");
            button.BackColor = isOn
                ? Color.FromArgb(46, 160, 67)
                : Color.FromArgb(125, 125, 125);
        }

        private int ReadDwordValue(string address)
        {
            int startRegister = int.Parse(address.Substring(1));
            OperateResult<ushort> low = _plcServer.ReadUInt16(address);
            OperateResult<ushort> high = _plcServer.ReadUInt16("D" + (startRegister + 1));
            if (!low.IsSuccess || !high.IsSuccess)
            {
                throw new InvalidOperationException("Không đọc được DWord tại " + address);
            }

            uint value = ((uint)high.Content << 16) | low.Content;
            return unchecked((int)value);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "logRBVS.ini");

            if (!File.Exists(filePath))
            {
                AddLog("Không tìm thấy file: " + filePath);
                return;
            }

            try
            {
                _locations.Clear();
                _locationDisplayLines.Clear();
                string[] lines = File.ReadAllLines(filePath);

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0)
                    {
                        continue;
                    }

                    string[] values = line.Split(',');
                    if (values.Length < 5)
                    {
                        throw new FormatException(
                            "Dòng " + (i + 1) + " phải có ít nhất 5 phần tử.");
                    }

                    decimal x;
                    decimal y;
                    decimal rz;
                    if (!decimal.TryParse(values[2].Trim(), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out x) ||
                        !decimal.TryParse(values[3].Trim(), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out y) ||
                        !decimal.TryParse(values[4].Trim(), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out rz))
                    {
                        throw new FormatException(
                            "Không đọc được X, Y, RZ tại dòng " + (i + 1) + ".");
                    }

                    int scaledX = ScaleCoordinate(x, i + 1, "X");
                    int scaledY = ScaleCoordinate(y, i + 1, "Y");
                    int scaledRz = ScaleCoordinate(rz, i + 1, "RZ");
                    _locations.Add(new[] { scaledX, scaledY, scaledRz });
                    _locationDisplayLines.Add(
                        (i + 1).ToString("D2", CultureInfo.InvariantCulture) +
                        " | X=" + x.ToString(CultureInfo.InvariantCulture) + " -> " + scaledX +
                        " | Y=" + y.ToString(CultureInfo.InvariantCulture) + " -> " + scaledY +
                        " | RZ=" + rz.ToString(CultureInfo.InvariantCulture) + " -> " + scaledRz);
                }

                _locationView.Lines = _locationDisplayLines.ToArray();
                AddLog("Đã nạp " + _locations.Count + " dòng logRBVS.ini (X/Y/RZ x10000)");
            }
            catch (Exception exception)
            {
                _locations.Clear();
                _locationDisplayLines.Clear();
                _locationView.Clear();
                AddLog("Lỗi đọc logRBVS.ini: " + exception.Message);
                MessageBox.Show(
                    exception.Message,
                    "logRBVS.ini không hợp lệ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private int ScaleCoordinate(decimal value, int lineNumber, string axis)
        {
            decimal scaledValue = decimal.Round(
                value * 10000m,
                0,
                MidpointRounding.AwayFromZero);

            if (scaledValue < int.MinValue || scaledValue > int.MaxValue)
            {
                throw new OverflowException(
                    axis + " tại dòng " + lineNumber + " vượt phạm vi Int32 sau khi nhân 10000.");
            }

            return decimal.ToInt32(scaledValue);
        }

        private void HighlightLocation(int index)
        {
            _locationView.SelectAll();
            _locationView.SelectionBackColor = Color.White;
            _locationView.SelectionColor = Color.Black;
            _locationView.SelectionLength = 0;

            if (index < 0 || index >= _locationDisplayLines.Count)
            {
                return;
            }

            int start = _locationView.GetFirstCharIndexFromLine(index);
            if (start < 0)
            {
                return;
            }

            _locationView.Select(start, _locationDisplayLines[index].Length);
            _locationView.SelectionBackColor = Color.Yellow;
            _locationView.SelectionColor = Color.Black;
            _locationView.SelectionLength = 0;
            _locationView.ScrollToCaret();
        }
        private async void RunClick(object sender, EventArgs e)
        {
            if (_runCancellation != null)
            {
                _runCancellation.Cancel();
                ResetPlcData();
                AddLog("Đã dừng và reset các thanh ghi PLC");
                return;
            }

            if (_plcServer == null)
            {
                MessageBox.Show(
                    "Hãy Connect PLC ảo trước.",
                    "PLC chưa kết nối",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (_locations.Count == 0)
            {
                MessageBox.Show(
                    "logRBVS.ini chưa có dữ liệu hợp lệ.",
                    "Không có vị trí",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            _runCancellation = new CancellationTokenSource();
            CancellationToken token = _runCancellation.Token;
            ((Button)sender).Text = "Stop";

            try
            {
                await RunLocationCycleAsync(token);
                AddLog("Hoàn tất một chu trình");
            }
            catch (OperationCanceledException)
            {
                AddLog("Đã dừng chu trình");
            }
            catch (Exception exception)
            {
                AddLog("Lỗi chu trình: " + exception.Message);
                MessageBox.Show(
                    exception.Message,
                    "Lỗi PLC",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _runCancellation.Dispose();
                _runCancellation = null;
                ((Button)sender).Text = "Run";
            }
        }

        private async Task RunLocationCycleAsync(CancellationToken token)
        {
            for (int i = 0; i < _locations.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                string requestBit = (i == 0) ? "D20.6" : "D20.7";
                if (i > 0)
                {
                    await WaitForBitAsync("D0.2", false, token);
                }

                WriteBit(requestBit, true);
                AddLog("Bật " + requestBit + ", chờ D0.2 = 1 (dòng " + (i + 1) + ")");
                bool acknowledgeReceived = await WaitForBitWithTimeoutAsync(
                    "D0.2", true, TimeSpan.FromSeconds(100), token);
                if (!acknowledgeReceived)
                {
                    AddLog("Timeout 100 giây chờ D0.2 = 1; tiếp tục gửi dữ liệu");
                }
                token.ThrowIfCancellationRequested();

                HighlightLocation(i);
                int[] location = _locations[i];
                WriteDword("D22", location[0]);
                WriteDword("D24", location[1]);
                WriteDword("D26", location[2]);

                AddLog(
                    "Đã ghi dòng " + (i + 1) +
                    ": X=" + location[0] +
                    ", Y=" + location[1] +
                    ", RZ=" + location[2]);
                WriteBit(requestBit, false);
            }

            await WaitForBitAsync("D0.2", false, token);
            await WaitForBitAsync("D0.6", true, token);
            WriteBit("D20.8", true);
            AddLog("Bật D20.8, chờ D0.6 = 0");
            await WaitForBitAsync("D0.6", false, token);
            WriteBit("D20.8", false);
        }
        private async Task<bool> WaitForBitWithTimeoutAsync(
            string address,
            bool expectedValue,
            TimeSpan timeout,
            CancellationToken token)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout)
            {
                token.ThrowIfCancellationRequested();

                OperateResult<bool[]> result = _plcServer.ReadBool(address, 1);
                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException(
                        "Đọc " + address + " thất bại: " + result.Message);
                }

                if (result.Content != null &&
                    result.Content.Length > 0 &&
                    result.Content[0] == expectedValue)
                {
                    return true;
                }

                TimeSpan remaining = timeout - stopwatch.Elapsed;
                TimeSpan pollDelay = remaining < TimeSpan.FromMilliseconds(50)
                    ? remaining
                    : TimeSpan.FromMilliseconds(50);
                if (pollDelay > TimeSpan.Zero)
                {
                    await Task.Delay(pollDelay, token);
                }
            }

            return false;
        }
        private async Task WaitForBitAsync(
            string address,
            bool expectedValue,
            CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();

                OperateResult<bool[]> result = _plcServer.ReadBool(address, 1);
                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException(
                        "Đọc " + address + " thất bại: " + result.Message);
                }

                if (result.Content != null &&
                    result.Content.Length > 0 &&
                    result.Content[0] == expectedValue)
                {
                    return;
                }

                await Task.Delay(50, token);
            }
        }

        private void WriteBit(string address, bool value)
        {
            OperateResult result = _plcServer.Write(
                address,
                new[] { value });

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    "Ghi " + address + " thất bại: " + result.Message);
            }
        }

        private async void ResetClick(object sender, EventArgs e)
        {
            if (_plcServer == null)
            {
                AddLog("Connect PLC trước khi reset");
                return;
            }

            if (_resetPulseRunning)
            {
                return;
            }

            _resetPulseRunning = true;
            Button resetButton = sender as Button;
            if (resetButton != null)
            {
                resetButton.Enabled = false;
            }

            try
            {
                WriteBit("D20.1", true);
                AddLog("Bật D20.1 trong 0.5 giây");
                await Task.Delay(500);
                WriteBit("D20.1", false);
                AddLog("Đã tắt D20.1");
            }
            catch (Exception exception)
            {
                if (_plcServer != null)
                {
                    try
                    {
                        WriteBit("D20.0", false);
                    }
                    catch
                    {
                        // Ignore a failed cleanup write if the server disconnected.
                    }
                }

                AddLog("Lỗi reset: " + exception.Message);
            }
            finally
            {
                _resetPulseRunning = false;
                if (resetButton != null && !resetButton.IsDisposed)
                {
                    resetButton.Enabled = true;
                }
            }
        }

        private void ResetPlcData()
        {
            if (_plcServer == null)
            {
                return;
            }

            ResetWord("D0");
            ResetWord("D20");
            for (int register = 22; register <= 27; register++)
            {
                ResetWord("D" + register);
            }
        }

        private void ResetWord(string address)
        {
            OperateResult result = _plcServer.Write(address, (ushort)0);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    "Reset " + address + " thất bại: " + result.Message);
            }
        }
        private void WriteDword(string address, int value)
        {
            uint unsignedValue = unchecked((uint)value);
            ushort lowWord = (ushort)(unsignedValue & 0xFFFF);
            ushort highWord = (ushort)(unsignedValue >> 16);
            int startRegister = int.Parse(address.Substring(1));
            string highAddress = "D" + (startRegister + 1);

            // Ghi word cao trước; ghi word thấp sau cùng để chốt giá trị tại địa chỉ bắt đầu.
            OperateResult highWrite = _plcServer.Write(highAddress, highWord);
            if (!highWrite.IsSuccess)
            {
                throw new InvalidOperationException(
                    "Ghi word cao tại " + highAddress + " thất bại: " + highWrite.Message);
            }

            OperateResult<ushort> highReadAfterWrite = _plcServer.ReadUInt16(highAddress);
            if (!highReadAfterWrite.IsSuccess || highReadAfterWrite.Content != highWord)
            {
                throw new InvalidOperationException(
                    "Sau khi ghi " + highAddress + "=" + highWord +
                    ", đọc lại được " +
                    (highReadAfterWrite.IsSuccess ? highReadAfterWrite.Content.ToString() : highReadAfterWrite.Message) + ".");
            }

            OperateResult lowWrite = _plcServer.Write(address, lowWord);
            if (!lowWrite.IsSuccess)
            {
                throw new InvalidOperationException(
                    "Ghi word thấp tại " + address + " thất bại: " + lowWrite.Message);
            }

            OperateResult<ushort> lowRead = _plcServer.ReadUInt16(address);
            if (!lowRead.IsSuccess || lowRead.Content != lowWord)
            {
                throw new InvalidOperationException(
                    "Sau khi ghi " + address + "=" + lowWord +
                    ", đọc lại được " +
                    (lowRead.IsSuccess ? lowRead.Content.ToString() : lowRead.Message) + ".");
            }

            OperateResult<ushort> highReadFinal = _plcServer.ReadUInt16(highAddress);
            if (!highReadFinal.IsSuccess || highReadFinal.Content != highWord)
            {
                throw new InvalidOperationException(
                    highAddress + " đã đổi sau khi ghi " + address + ". Cần " + highWord +
                    ", đọc được " +
                    (highReadFinal.IsSuccess ? highReadFinal.Content.ToString() : highReadFinal.Message) + ".");
            }

            uint readBack = ((uint)highReadFinal.Content << 16) | lowRead.Content;
            AddLog("DWord " + address + "-" + highAddress +
                   " đọc lại: thấp=" + lowRead.Content +
                   ", cao=" + highReadFinal.Content +
                   ", giá trị=" + unchecked((int)readBack));
        }
        private void ConnectClick(object sender, EventArgs e)
        {
            if (_plcServer != null)
            {
                StopServer();
                return;
            }

            IPAddress parsedAddress;
            int portNumber;

            if (!IPAddress.TryParse(_address.Text.Trim(), out parsedAddress))
            {
                MessageBox.Show(
                    "Address phải là địa chỉ IPv4 hoặc IPv6 hợp lệ.",
                    "Invalid address",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(_port.Text.Trim(), out portNumber) ||
                portNumber < 1 || portNumber > 65535)
            {
                MessageBox.Show(
                    "Port phải nằm trong khoảng 1 đến 65535.",
                    "Invalid port",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!_isServer.Checked)
            {
                MessageBox.Show(
                    "Chế độ Client chưa được cấu hình. Hãy bật Server để khởi động PLC ảo.",
                    "Client mode",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                _plcServer = new MelsecMcServer(true);
                _plcServer.ServerStart(portNumber);
                _registerStatusTimer.Start();
                UpdateRegisterStatus();

                _status.Text = "Virtual Mitsubishi MC PLC running at " +
                               _address.Text.Trim() + ":" + portNumber;
                ((Button)sender).Text = "Disconnect";
                AddLog("PLC ảo Mitsubishi MC đã khởi động tại cổng " + portNumber);
            }
            catch (Exception exception)
            {
                _registerStatusTimer.Stop();
                if (_plcServer != null)
                {
                    _plcServer.Dispose();
                    _plcServer = null;
                }
                UpdateRegisterStatus();

                MessageBox.Show(
                    "Không thể khởi động PLC ảo: " + exception.Message,
                    "Connect error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                AddLog("Lỗi khởi động PLC ảo: " + exception.Message);
            }
        }

        private void StopServer()
        {
            _registerStatusTimer.Stop();
            try
            {
                ResetPlcData();
                _plcServer.ServerClose();
                _plcServer.Dispose();
                _plcServer = null;
                UpdateRegisterStatus();

                _status.Text = "Disconnected";
                foreach (Control control in Controls)
                {
                    var button = control as Button;
                    if (button != null && button.Text == "Disconnect")
                    {
                        button.Text = "Connect";
                        break;
                    }
                }

                AddLog("PLC ảo đã dừng");
            }
            catch (Exception exception)
            {
                AddLog("Lỗi khi dừng PLC ảo: " + exception.Message);
            }
        }

        private void AddLog(string message)
        {
            _log.Items.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + message);
            if (_log.Items.Count > 0)
            {
                _log.TopIndex = _log.Items.Count - 1;
            }
        }

        private void Form1Closing(object sender, FormClosingEventArgs e)
        {
            _registerStatusTimer.Stop();
            if (_runCancellation != null)
            {
                _runCancellation.Cancel();
            }
            if (_plcServer != null)
            {
                try
                {
                    ResetPlcData();
                }
                catch (Exception exception)
                {
                    AddLog("Không reset được thanh ghi khi đóng form: " + exception.Message);
                }
                _plcServer.ServerClose();
                _plcServer.Dispose();
                _plcServer = null;
            }
        }
    }
}
