using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using TorProxy.Listener;
using TorProxy.Network;
using TorProxy.Proxy;
using TorProxy.Relays;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TorProxy.GUI
{


    // I still havent decided whenever I should call them tor nodes or tor relays

    internal partial class TorControl : Form
    {

        private static TorControl? _instance;

        public static TorControl Instance
        {
            get
            {
                _instance ??= new TorControl();
                return _instance;
            }
        }

        private readonly System.Windows.Forms.Timer filterTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer statsUpdateTimer = new System.Windows.Forms.Timer();

        private string _commandDispatch = string.Empty;

        public TorControl()
        {
            _instance = this;
            InitializeComponent();
            bridge_type_combobox.SelectedIndex = 1;
            WindowState = FormWindowState.Minimized;
            MaximizeBox = false;
            ControlBox = false;

            if (!TorService.Instance.ExistsConfigurationValue("Bridge")) bridges_list_textbox.Lines = Configuration.Instance.Get("DefaultBridges");
            else bridges_list_textbox.Lines = TorService.Instance.GetConfigurationValue("Bridge");
            use_bridges_checkbox.Checked = TorService.Instance.GetConfigurationValue("UseBridges")[0] == "1";

            bridge_type_combobox.SelectedIndex = 3;
            if (bridges_list_textbox.Lines.Any(x => x.StartsWith("obfs4"))) bridge_type_combobox.SelectedIndex = 0;
            if (bridges_list_textbox.Lines.Any(x => x.StartsWith("webtunnel"))) bridge_type_combobox.SelectedIndex = 1;
            if (bridges_list_textbox.Lines.Any(x => x.StartsWith("obfs4")) && bridges_list_textbox.Lines.Any(x => x.StartsWith("webtunnel"))) bridge_type_combobox.SelectedIndex = 2;

            TorService.Instance.OnNewDeadBridge += TorService_OnNewDeadBridge;
            TorService.Instance.OnNewWorkingBridge += TorService_OnNewWorkingBridge;
            TorService.Instance.OnStartupStatusChange += TorService_OnStartupStatusChange;
            TorService.Instance.OnNewMessage += TorService_OnNewMessage;
            TorService.Instance.OnBridgeProxyExhaustion += TorService_OnBridgeProxyExhaustion;
            TorService.Instance.OnStatusChange += TorService_OnStatusChange;

            filterTimer.Interval = (int)(filter_reload_time_textbox.Value * 1000);
            filterTimer.Tick += FilterTimer_Tick;
            statsUpdateTimer.Interval = 1000;
            statsUpdateTimer.Tick += StatsUpdateTimer_Tick;
            statsUpdateTimer.Start();
            UpdateExitRelaysDisplay();
            UpdateGuardRelaysDisplay();
            if (TorService.Instance.GetConfigurationValue("ExitNodes") != Array.Empty<string>())
            {
                specify_exit_nodes_checkbox.Checked = true;
                exit_node_textbox.Text = TorService.Instance.GetConfigurationValue("ExitNodes").FirstOrDefault();
                SelectExitRelayInTextbox();
            }
            if (TorService.Instance.GetConfigurationValue("EntryNodes") != Array.Empty<string>())
            {
                specify_guard_nodes_checkbox.Checked = true;
                guard_node_textbox.Text = TorService.Instance.GetConfigurationValue("EntryNodes").FirstOrDefault();
                SelectGuardRelayInTextbox();
            }
            FormClosed += TorControl_FormClosed;
            FormClosing += TorControl_FormClosing;
        }

        private void TorControl_FormClosing(object? sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Visible = false;
            WindowState = FormWindowState.Minimized;
        }

        private void TorControl_FormClosed(object? sender, FormClosedEventArgs e)
        {
            Unhook();
        }

        public void Unhook()
        {
            TorService.Instance.OnNewDeadBridge -= TorService_OnNewDeadBridge;
            TorService.Instance.OnNewWorkingBridge -= TorService_OnNewWorkingBridge;
            TorService.Instance.OnStartupStatusChange -= TorService_OnStartupStatusChange;
            TorService.Instance.OnNewMessage -= TorService_OnNewMessage;
            TorService.Instance.OnBridgeProxyExhaustion -= TorService_OnBridgeProxyExhaustion;
            TorService.Instance.OnStatusChange -= TorService_OnStatusChange;
        }

        private void StatsUpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (TorServiceListener.SpeedMonitor == null) return;
            int downloadSpeed = ((int)(TorServiceListener.SpeedMonitor.BytesReceiveSpeed / 1024));
            int uploadSpeed = ((int)(TorServiceListener.SpeedMonitor.BytesSentSpeed / 1024));
            download_speed_label.Text = "Download speed: " + downloadSpeed.ToString() + " Kb/S";
            packets_download_speed_label.Text = "Packets download speed: " + ((int)(TorServiceListener.SpeedMonitor.PacketsReceiveSpeed)).ToString() + " P/S";
            upload_speed_label.Text = "Upload speed: " + uploadSpeed.ToString() + " Kb/S";
            packets_upload_speed_label.Text = "Packets upload speed: " + ((int)(TorServiceListener.SpeedMonitor.PacketsSentSpeed)).ToString() + " P/S";
            control_port_connected_label.Text = "Is control port usable: " + (TorService.Instance.TorController.IsUsable ? "yes" : "no");

            Console.Title = "Status - " + TorService.Instance.StartupStatus + " <> ↓" + downloadSpeed + " Kb/S" + " <> ↑" + uploadSpeed + "Kb/S";

            if (_commandDispatch == string.Empty) return;
            // That's some certified bullshit
            Thread commandExecThread = new(() =>
            {
                StringBuilder resp = new();
                if (!TorService.Instance.TorController.IsUsable) return;
                try
                {
                    if (_commandDispatch.Trim().ToUpper() == "CLS")
                    {
                        Invoke(() =>
                        {
                            command_resp_textbox.Text = string.Empty;
                        });
                        return;
                    }
                    TorService.Instance.TorController.SendLine(_commandDispatch);
                    _commandDispatch = string.Empty;
                    while (true)
                    {
                        if (!TorService.Instance.TorController.IsConnected) return;
                        resp = resp.AppendLine(TorService.Instance.TorController.ReadLine());
                    }
                }
                catch (Exception)
                {
                }
                Invoke(() =>
                {
                    if (resp.Length != 0) command_resp_textbox.Text += Environment.NewLine + resp.ToString();
                });
            });
            commandExecThread.Start();
        }

        private void TorService_OnStatusChange(object? sender, EventArgs e)
        {
            Invoke(() =>
            {
                switch (TorService.Instance.Status)
                {
                    case ProxyStatus.Running:
                        break;

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
                log_textbox.Lines = log_textbox.Lines.ToList().Append(e.Message.Substring(20)).ToArray();
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
            bridges_list_textbox.Lines = Configuration.Instance.Get("DefaultBridges");
        }

        private void connect_button_Click(object sender, EventArgs e)
        {
            if (TorService.Instance.ProxyRunning) return;
            if (specify_guard_nodes_checkbox.Checked && use_bridges_checkbox.Checked)
            {
                DisplayError("You cant specify guard nodes when using bridges!");
                return;
            }
            log_textbox.Clear();
            error_label.Visible = false;
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
                        index = pulledBridges.IndexOf(bridgeLine);
                        if (bridgeLine.StartsWith("http"))
                        {
                            pulledBridges.Remove(bridgeLine);
                            status_label.Text = "Status: Downloading bridges...";
                            try
                            {
                                pulledBridges.InsertRange(index, Utils.Download(bridgeLine).Split("\n"));
                            }
                            catch
                            {
                                DisplayError("Cant download: " + bridgeLine);
                                connect_button.Enabled = true;
                                return;
                            }
                        }
                        if (File.Exists(bridgeLine))
                        {
                            try
                            {
                                pulledBridges.InsertRange(index, File.ReadAllLines(bridgeLine));
                            }
                            catch
                            {
                                DisplayError("Cant read: " + bridgeLine);
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
                Utils.Random.Shuffle(finalBridges);
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

            TorService.Instance.SetConfigurationValue("ExitNodes", (specify_exit_nodes_checkbox.Checked && !string.IsNullOrWhiteSpace(exit_node_textbox.Text)) ? exit_node_textbox.Text : string.Empty);
            TorService.Instance.SetConfigurationValue("EntryNodes", (specify_guard_nodes_checkbox.Checked && !string.IsNullOrWhiteSpace(guard_node_textbox.Text)) ? guard_node_textbox.Text : string.Empty);

            TorService.Instance.StartTorProxy();
        }

        private void disconnect_button_Click(object sender, EventArgs e)
        {
            if (!TorService.Instance.ProxyRunning) return;
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

        private void specify_exit_nodes_checkbox_CheckedChanged(object sender, EventArgs e)
        {
            if (specify_exit_nodes_checkbox.Checked)
            {
                exit_nodes_list.Enabled = true;
                exit_node_textbox.Enabled = true;
                fast_exit_relays_flag_checkbox.Enabled = true;
                stable_exit_relays_flag_checbox.Enabled = true;
            }
            else
            {
                exit_nodes_list.Enabled = false;
                exit_node_textbox.Enabled = false;
                fast_exit_relays_flag_checkbox.Enabled = false;
                stable_exit_relays_flag_checbox.Enabled = false;
            }
        }

        private void update_tor_relays_button_Click(object sender, EventArgs e)
        {
            error_label.Visible = false;
            update_tor_relays_button.Enabled = false;
            Thread downloaderThread = new(() =>
            {
                string startItem = exit_node_textbox.Text;
                foreach (string url in Configuration.Instance.Get("RelayMirrors"))
                {
                    try
                    {
                        File.Delete(RelayDistributor.Instance.FilePath + ".backup");
                        File.Copy(RelayDistributor.Instance.FilePath, RelayDistributor.Instance.FilePath + ".backup");
                        File.Delete(RelayDistributor.Instance.FilePath);
                        Utils.DownloadToFile(url, RelayDistributor.Instance.FilePath);
                    }
                    catch (Exception)
                    {
                        Instance.Invoke(() =>
                        {
                            DisplayError("Cant download: " + url);
                        });
                    }
                    try
                    {
                        RelayDistributor.Instance.Read();
                    }
                    catch (Exception)
                    {
                        Instance.Invoke(() =>
                        {
                            DisplayError("Unable to read relay dump");
                        });
                        File.Copy(RelayDistributor.Instance.FilePath + ".backup", RelayDistributor.Instance.FilePath);
                    }
                }
                Instance.Invoke(() =>
                {
                    UpdateExitRelaysDisplay();
                    UpdateGuardRelaysDisplay();
                    update_tor_relays_button.Enabled = true;

                });
            }
            );
            downloaderThread.Start();
        }

        private void UpdateExitRelaysDisplay()
        {
            string prevState = exit_node_textbox.Text;
            List<string> usedFlags = new() { "Exit", "Running", "Valid" };
            List<string> avoidFlags = new() { "BadExit" };
            if (stable_exit_relays_flag_checbox.Checked) usedFlags.Add("Stable");
            if (fast_exit_relays_flag_checkbox.Checked) usedFlags.Add("Fast");
            List<string> ips = RelayDistributor.Instance.GetRelaysWithFlags(usedFlags.ToArray()).GetRelaysWithoutFlags(avoidFlags.ToArray()).ToList().ConvertAll(x => x.Country + " " + x.Addresses.FirstOrDefault());
            Utils.Random.Shuffle(ips);
            exit_nodes_list.DataSource = ips;
            exit_node_textbox.Text = prevState;
            SelectExitRelayInTextbox();
            exit_nodes_count_label.Text = "Exit nodes: " + ips.Count.ToString();
        }

        private void UpdateGuardRelaysDisplay()
        {
            string prevState = guard_node_textbox.Text;
            List<string> usedFlags = new() { "Guard", "Running", "Valid" };
            List<string> avoidFlags = new() { "BadExit" };
            if (stable_guard_relays_flag_checbox.Checked) usedFlags.Add("Stable");
            if (fast_guard_relays_flag_checkbox.Checked) usedFlags.Add("Fast");
            List<string> ips = RelayDistributor.Instance.GetRelaysWithFlags(usedFlags.ToArray()).GetRelaysWithoutFlags(avoidFlags.ToArray()).ToList().ConvertAll(x => x.Country + " " + x.Addresses.FirstOrDefault());
            Utils.Random.Shuffle(ips);
            guard_nodes_list.DataSource = ips;
            guard_node_textbox.Text = prevState;
            SelectGuardRelayInTextbox();
            guard_node_count_label.Text = "Guard nodes: " + ips.Count.ToString();
        }

        private void SelectExitRelayInTextbox()
        {
            if (Utils.Ipv4AddressSelector.IsMatch(exit_node_textbox.Text))
            {
                Relay? find = RelayDistributor.Instance.FindRelayByIp(exit_node_textbox.Text);
                if (find != null && exit_nodes_list.Items.Contains(find?.Country + " " + find?.Addresses[0]))
                {
                    exit_nodes_list.SetSelected(exit_nodes_list.Items.IndexOf(find?.Country + " " + find?.Addresses[0]), true);
                }
                exit_node_textbox.Text = find?.Addresses[0];
            }
        }
        private void SelectGuardRelayInTextbox()
        {
            if (Utils.Ipv4AddressSelector.IsMatch(guard_node_textbox.Text))
            {
                Relay? find = RelayDistributor.Instance.FindRelayByIp(guard_node_textbox.Text);
                if (guard_nodes_list.Items.Contains(find?.Country + " " + find?.Addresses[0]))
                {
                    guard_nodes_list.SetSelected(guard_nodes_list.Items.IndexOf(find?.Country + " " + find?.Addresses[0]), true);
                }
                guard_node_textbox.Text = find?.Addresses[0];
            }
        }

        private void exit_nodes_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            exit_node_textbox.Text = exit_nodes_list.SelectedItem?.ToString()?.Split(" ").LastOrDefault();
        }

        private void fast_exit_relays_flag_checkbox_Click(object sender, EventArgs e)
        {
            UpdateExitRelaysDisplay();
        }

        private void stable_exit_relays_flag_checbox_Click(object sender, EventArgs e)
        {
            UpdateExitRelaysDisplay();
        }

        private void specify_guard_nodes_checkbox_Click(object sender, EventArgs e)
        {
            if (specify_guard_nodes_checkbox.Checked)
            {
                guard_nodes_list.Enabled = true;
                guard_node_textbox.Enabled = true;
                fast_guard_relays_flag_checkbox.Enabled = true;
                stable_guard_relays_flag_checbox.Enabled = true;
            }
            else
            {
                guard_nodes_list.Enabled = false;
                guard_node_textbox.Enabled = false;
                fast_guard_relays_flag_checkbox.Enabled = false;
                stable_guard_relays_flag_checbox.Enabled = false;
            }
        }

        private void fast_guard_relays_flag_checkbox_Click(object sender, EventArgs e)
        {
            UpdateGuardRelaysDisplay();
        }

        private void stable_guard_relays_flag_checbox_Click(object sender, EventArgs e)
        {
            UpdateGuardRelaysDisplay();
        }

        private void guard_nodes_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            guard_node_textbox.Text = guard_nodes_list.SelectedItem?.ToString()?.Split(" ").LastOrDefault();
        }

        private void command_texbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                _commandDispatch = command_texbox.Text;
                command_texbox.Text = string.Empty;
            }
        }

        private void filter_reload_time_textbox_ValueChanged(object sender, EventArgs e)
        {
            if (filterTimer.Enabled)
            {
                filterTimer.Stop();
                filterTimer.Interval = (int)(filter_reload_time_textbox.Value * 1000);
                filterTimer.Start();
            }
            else
            {
                filterTimer.Interval = (int)(filter_reload_time_textbox.Value * 1000);
            }
        }
    }
}
