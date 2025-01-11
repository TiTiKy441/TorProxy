using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using TorProxy.Proxy;
using static System.Windows.Forms.DataFormats;

namespace TorProxy
{
    public partial class SettingsWindow : Form
    {

        private static SettingsWindow? _instance;

        public static SettingsWindow Instance
        {
            get
            {
                _instance ??= new SettingsWindow();
                return _instance;
            }
        }

        private readonly System.Windows.Forms.Timer filterTimer = new System.Windows.Forms.Timer();

        public readonly static string[] DefaultBridges = new string[] {
            "https://github.com/scriptzteam/Tor-Bridges-Collector/blob/main/bridges-webtunnel?raw=true",
            "https://github.com/scriptzteam/Tor-Bridges-Collector/blob/main/bridges-obfs4?raw=true"
        };

        public SettingsWindow()
        {
            _instance = this;
            InitializeComponent();
            bridge_type_combobox.SelectedIndex = 1;
            WindowState = FormWindowState.Minimized;
            MaximizeBox = false;
            ControlBox = false;
            if (!TorService.Instance.ExistsConfigurationValue("Bridge")) bridges_list_textbox.Lines = DefaultBridges;
            else bridges_list_textbox.Lines = TorService.Instance.GetConfigurationValue("Bridge");
            use_bridges_checkbox.Checked = TorService.Instance.GetConfigurationValue("UseBridges")[0] == "1";
            TorService.Instance.OnNewDeadBridge += TorService_OnNewDeadBridge;
            TorService.Instance.OnNewWorkingBridge += TorService_OnNewWorkingBridge;
            TorService.Instance.OnStartupStatusChange += TorService_OnStartupStatusChange;
            TorService.Instance.OnNewMessage += TorService_OnNewMessage;
            TorService.Instance.OnBridgeProxyExhaustion += TorService_OnBridgeProxyExhaustion;
            TorService.Instance.OnStatusChange += TorService_OnStatusChange;
            filterTimer.Interval = (int)(filter_reload_time_textbox.Value * 1000);
            filterTimer.Tick += FilterTimer_Tick;
        }

        private void TorService_OnStatusChange(object? sender, EventArgs e)
        {
            Invoke(() =>
            {
                switch (TorService.Instance.Status)
                {
                    case ProxyStatus.Disabled:
                        status_label.Text = "Status: NoProcess";
                        connect_button.Enabled = true;
                        disconnect_button.Enabled = false;
                        filterTimer.Stop();
                        break;

                    case ProxyStatus.Starting:
                        disconnect_button.Enabled = true;
                        connect_button.Enabled = false;
                        break;
                }
            });
        }

        private bool IsInsufficientBridgesState()
        {
            return (TorService.Instance.WorkingBridges.Count < bridges_count_textbox.Value
                || (bridge_type_combobox.SelectedIndex == 2 && !TorService.Instance.WorkingBridges.Any(x => x.StartsWith("obfs4")))
                || (bridge_type_combobox.SelectedIndex == 2 && !TorService.Instance.WorkingBridges.Any(x => x.StartsWith("webtunnel")))
                || (bridge_type_combobox.SelectedIndex == 0 && !TorService.Instance.WorkingBridges.Any(x => x.StartsWith("obfs4")))
                || (bridge_type_combobox.SelectedIndex == 1 && !TorService.Instance.WorkingBridges.Any(x => x.StartsWith("webtunnel"))));
        }

        private void TorService_OnBridgeProxyExhaustion(object? sender, EventArgs e)
        {
            Invoke(() =>
            {
                if (IsInsufficientBridgesState())
                {
                    filterTimer.Stop();
                    filterTimer.Start();
                    return;
                }
                TorService.Instance.ReloadWithWorkingBridges();
            });
        }

        private void TorService_OnNewMessage(object? sender, NewMessageEventArgs e)
        {
            Invoke(() =>
            {
                if (log_textbox.Lines.Length > 50) log_textbox.Lines = log_textbox.Lines.ToList().Skip(20).ToArray();
                log_textbox.Lines = log_textbox.Lines.ToList().Append(e.Message).ToArray();
                log_textbox.SelectionStart = log_textbox.Text.Length - log_textbox.Lines[^1].Length;
                log_textbox.ScrollToCaret();
            });
        }

        private void TorService_OnStartupStatusChange(object? sender, EventArgs e)
        {
            Invoke(() =>
            {
                status_label.Text = "Status: " + TorService.Instance.StartupStatus;
            });
        }

        private void FilterTimer_Tick(object? sender, EventArgs e)
        {
            if (!TorService.Instance.ProxyRunning) return;
            if (IsInsufficientBridgesState()) return;

            if (TorService.Instance.WorkingBridges.Count == TorService.Instance.GetConfigurationValue("Bridge").Length)
            {
                current_bridges_list.Items.Clear();
                current_bridges_list.Items.AddRange(TorService.Instance.WorkingBridges.ToArray());
                filterTimer.Stop();
                return;
            }
            TorService.Instance.ReloadWithWorkingBridges();

            current_bridges_list.Items.Clear();
            current_bridges_list.Items.AddRange(TorService.Instance.GetConfigurationValue("Bridge"));
            UpdateBridgeStats();
        }

        private void TorService_OnNewWorkingBridge(object? sender, NewBridgeEventArgs e)
        {
            Invoke(() =>
            {
                UpdateBridgeStats();
                if (!current_bridges_list.Items.Contains(e.Bridge)) current_bridges_list.Items.Add(e.Bridge);
            });
        }

        private void TorService_OnNewDeadBridge(object? sender, NewBridgeEventArgs e)
        {
            Invoke(() =>
            {
                UpdateBridgeStats();
                if (current_bridges_list.Items.Contains(e.Bridge)) current_bridges_list.Items.Remove(e.Bridge);
            });
        }

        private void UpdateBridgeStats()
        {
            total_bridges_count_label.Text = "Total bridges added: " + TorService.Instance.GetConfigurationValue("Bridge").Length;
            unfiltered_bridges_count_label.Text = "Unfiltered bridges: " + (TorService.Instance.GetConfigurationValue("Bridge").Length - TorService.Instance.WorkingBridges.Count - TorService.Instance.DeadBridges.Count).ToString();
            bridges_ration_label.Text = "Working/Dead bridges ratio: " + TorService.Instance.WorkingBridges.Count.ToString() + "/" + TorService.Instance.DeadBridges.Count.ToString();
        }

        private void reset_bridges_button_Click(object sender, EventArgs e)
        {
            bridges_list_textbox.Clear();
            bridges_list_textbox.Lines = DefaultBridges;
        }

        private void connect_button_Click(object sender, EventArgs e)
        {
            if (TorService.Instance.ProxyRunning) return;
            log_textbox.Clear();
            error_label.Visible = false;
            TorService.EnableProxy(false);
            TorService.Instance.SetConfigurationValue("UseBridges", use_bridges_checkbox.Checked ? "1" : "0");
            if (use_bridges_checkbox.Checked)
            {
                List<string> pulledBridges = bridges_list_textbox.Lines.ToList();
                List<string> finalBridges = new();
                int index;
                using (HttpClient client = new())
                {
                    connect_button.Enabled = false;
                    foreach (string bridgeLine in bridges_list_textbox.Lines)
                    {
                        if (bridgeLine.StartsWith("http"))
                        {
                            index = pulledBridges.IndexOf(bridgeLine);
                            pulledBridges.Remove(bridgeLine);
                            status_label.Text = "Status: Downloading bridges...";
                            try
                            {
                                pulledBridges.InsertRange(index, client.Send(new HttpRequestMessage(HttpMethod.Get, bridgeLine)).Content.ReadAsStringAsync().Result.Split("\n"));
                            }
                            catch
                            {
                                DisplayError("Cant download: " + bridgeLine);
                                connect_button.Enabled = true;
                                return;
                            }
                        }
                    }
                    connect_button.Enabled = true;
                }
                foreach (string bridgeLine in pulledBridges)
                {
                    if (bridgeLine.StartsWith("Bridge"))
                    {
                        finalBridges.Add(bridgeLine.Split(" ", 2)[1].Trim());
                    }
                    if (bridgeLine.StartsWith("obfs4"))
                    {
                        if (bridge_type_combobox.SelectedIndex == 1) continue;
                        finalBridges.Add(bridgeLine.Trim());
                    }
                    if (bridgeLine.StartsWith("webtunnel"))
                    {
                        if (bridge_type_combobox.SelectedIndex == 0) continue;
                        finalBridges.Add(bridgeLine.Trim());
                    }
                }
                TorService.Instance.SetConfigurationValue("Bridge", finalBridges.ToArray());
                UpdateBridgeStats();
                current_bridges_list.Items.Clear();
                current_bridges_list.Items.AddRange(finalBridges.ToArray());
                total_bridges_count_label.Text = "Total bridges added: " + finalBridges.Count.ToString();
                if (finalBridges.Count < bridges_count_textbox.Value)
                {
                    DisplayError("Not enough bridges was added!");
                    return;
                }
                switch (bridge_type_combobox.SelectedIndex)
                {
                    case 0:
                        if (!finalBridges.Any(x => x.StartsWith("obfs4")))
                        {
                            DisplayError("No obfs4 bridges was found!");
                            return;
                        }
                        break;

                    case 1:
                        if (!finalBridges.Any(x => x.StartsWith("webtunnel")))
                        {
                            DisplayError("No webtunnel bridges was found!");
                            return;
                        }
                        break;

                    case 2:
                        if (!finalBridges.Any(x => x.StartsWith("obfs4")))
                        {
                            DisplayError("No obfs4 bridges was found!");
                            return;
                        }
                        if (!finalBridges.Any(x => x.StartsWith("webtunnel")))
                        {
                            DisplayError("No webtunnel bridges was found!");
                            return;
                        }
                        break;
                }
                filterTimer.Start();
            }
            TorService.Instance.StartTorProxy();
        }

        private void disconnect_button_Click(object sender, EventArgs e)
        {
            if (!TorService.Instance.ProxyRunning) return;
            TorService.EnableProxy(false);
            TorService.Instance.StopTorProxy();
            TorService.Instance.WaitForEnd();
        }

        private void bridge_type_combobox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void DisplayError(string text)
        {
            error_label.Text = text;
            error_label.Visible = true;
        }

        private void current_bridges_list_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                StringBuilder copy_buffer = new StringBuilder();
                foreach (object item in current_bridges_list.SelectedItems)
                {
                    copy_buffer.AppendLine(item.ToString());
                }
                if (copy_buffer.Length > 0)
                {
                    Clipboard.SetDataObject(copy_buffer.ToString());
                }
            }
        }

        private void copy_all_button_Click(object sender, EventArgs e)
        {
            if (current_bridges_list.Items.Count > 0)
            {
                Clipboard.SetDataObject(string.Join(Environment.NewLine, current_bridges_list.Items.Cast<string>()));
            }
        }

        private void copy_obfs4_button_Click(object sender, EventArgs e)
        {
            StringBuilder outputString = new();
            foreach (string item in current_bridges_list.Items)
            {
                if (item.StartsWith("obfs4")) outputString.AppendLine(item);
            }
            if (outputString.Length > 0)
            {
                Clipboard.SetDataObject(outputString.ToString());
            }
        }

        private void copy_webtunnel_Click(object sender, EventArgs e)
        {
            StringBuilder outputString = new();
            foreach (string item in current_bridges_list.Items)
            {
                if (item.StartsWith("webtunnel")) outputString.AppendLine(item);
            }
            if (outputString.Length > 0)
            {
                Clipboard.SetDataObject(outputString.ToString());
            }
        }
    }
}
