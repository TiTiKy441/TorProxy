using Microsoft.Win32;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TorProxy.Proxy;

namespace TorProxy
{
    partial class Settings
    {
        private System.ComponentModel.IContainer components = null;

        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;
        private ToolStripDropDownButton settingsDropdown;
        private string BridgesCollectorGithub = "https://github.com/scriptzteam/Tor-Bridges-Collector/blob/main/bridges-webtunnel?raw=true";
        private System.Windows.Forms.Timer filterTimer = new System.Windows.Forms.Timer()
        {
            Interval = 1000 * 30, // 0.5min
        };

        private List<int> runningBridgesCount = new();

        public Settings()
        {
            Name = "TorProxy";
            Text = "TorProxy";

            TorService.EnableProxy(false);
            TorService.Instance.OnStartupStatusChange += TorService_OnStartupStatusChange;
            TorService.Instance.OnStatusChange += TorService_OnStatusChange;

            InitializeComponent();

            settingsDropdown = new ToolStripDropDownButton()
            {
                Text = "Bridges",
                Name = "bridges_settings_dropdown",
                DropDownItems =
                {
                    new ToolStripMenuItem()
                    {
                        Name = "use_bridges_button",
                        Checked = (TorService.Instance.GetConfigurationValue("UseBridges")[0] == "1"),
                        CheckOnClick = true,
                        Text = "Use bridges"
                    },

                    new ToolStripTextBox()
                    {
                        Name = "bridges_list",
                        Text = BridgesCollectorGithub,
                        Multiline = true,
                        Enabled = true,
                    },

                    new ToolStripButton()
                    {
                        Name = "reset_bridges_button",
                        Text = "Reset bridges",
                        Enabled = true,
                    },
                    new ToolStripMenuItem()
                    {
                        Name = "filter_running_button",
                        Checked = true,
                        CheckOnClick = true,
                        Text = "Stable filter"
                    },
                    
                    new ToolStripTextBox()
                    {
                        Name = "filter_update",
                        Text = (filterTimer.Interval / 1000).ToString(),
                        Multiline = false,
                        Enabled = true,
                    },
                },
            };

            contextMenu = new ContextMenuStrip()
            {
                Items =
                {
                    new ToolStripButton()
                    {
                        Name = "exit_button",
                        Text = "Exit",
                        Checked = false,
                    },
                    new ToolStripMenuItem()
                    {
                        Name = "use_proxy_button",
                        Text = "Enable",
                        Checked = false,
                        CheckOnClick = true,
                    },
                    new ToolStripButton()
                    {
                        Name = "connect_button",
                        Text = "Connect",
                    },
                    new ToolStripButton()
                    {
                        Name = "disconnect_button",
                        Text = "Disconnect",
                    },
                    new ToolStripTextBox()
                    {
                        Name = "log_textbox",
                        AcceptsTab = false,
                        Enabled = false,
                        Text = "NoProcess",
                    },

                    settingsDropdown,
                }
            };

            notifyIcon = new NotifyIcon()
            {
                ContextMenuStrip = contextMenu,
                Icon = new Icon("images/icon_disconnected.ico"),
                Text = "Tor proxy",
                Visible = true,
            };

            contextMenu.Items.Find("exit_button", false)[0].Click += ExitButton_Click;
            contextMenu.Items.Find("use_proxy_button", false)[0].Click += UseProxyButton_Click;
            contextMenu.Items.Find("connect_button", false)[0].Click += ConnectButton_Click;
            contextMenu.Items.Find("disconnect_button", false)[0].Click += DisconnectButton_Click;
            settingsDropdown.DropDownItems.Find("use_bridges_button", false)[0].Click += UseBridges_Click;
            settingsDropdown.DropDownItems.Find("reset_bridges_button", false)[0].Click += ResetBridges_Click;
            settingsDropdown.DropDownItems.Find("filter_update", false)[0].TextChanged += FilterUpdateTimeTextbox_TextChanged;
            Size = new Size(0, 0);
            Visible = false;
            ShowInTaskbar = false;
            Hide();
            WindowState = FormWindowState.Minimized;
            FormClosed += Settings_FormClosed;
            filterTimer.Tick += FilterTimer_Reload;
            filterTimer.Stop();
        }

        private void FilterUpdateTimeTextbox_TextChanged(object sender, EventArgs e)
        {
            if (Int32.TryParse(settingsDropdown.DropDownItems.Find("filter_update", false)[0].Text, out int newTime))
            {
                filterTimer.Interval = newTime * 1000;
            }
            else
            {
                settingsDropdown.DropDownItems.Find("filter_update", false)[0].Text = (filterTimer.Interval / 1000).ToString();
            }
        }

        private void FilterTimer_Reload(object sender, EventArgs e)
        {
            runningBridgesCount.Add(TorService.Instance.WorkingBridges.Count);
            if (runningBridgesCount.Count >= 2 && runningBridgesCount[^2] == runningBridgesCount[^1])
            {
                filterTimer.Stop();
                return;
            }
            TorService.Instance.ReloadWithWorkingBridges();
        }

        private void ResetBridges_Click(object sender, EventArgs e)
        {
            settingsDropdown.DropDownItems.Find("bridges_list", false)[0].Text = BridgesCollectorGithub;
        }

        private void UseBridges_Click(object sender, EventArgs e)
        {
            TorService.Instance.SetConfigurationValue("UseBridges", ((ToolStripMenuItem)settingsDropdown.DropDownItems.Find("use_bridges_button", false)[0]).Checked ? "1" : "0");

            if (TorService.Instance.Status == ProxyStatus.Running)
            {
                TorService.Instance.StopTorProxy();
                TorService.Instance.WaitForEnd();
            }
        }

        private void TorService_OnStatusChange(object sender, EventArgs e)
        {
            Invoke(new MethodInvoker(delegate
            {
                switch (TorService.Instance.Status)
                {
                    case ProxyStatus.Running:
                        notifyIcon.Icon = new Icon("images/icon_connected.ico");
                        contextMenu.Items.Find("use_proxy_button", false)[0].Enabled = true;
                        ((ToolStripMenuItem)contextMenu.Items.Find("use_proxy_button", false)[0]).Checked = true;
                        TorService.EnableProxy(true);
                        contextMenu.Items.Find("disconnect_button", false)[0].Enabled = true;
                        contextMenu.Items.Find("connect_button", false)[0].Enabled = false;
                        settingsDropdown.DropDownItems.Find("reset_bridges_button", false)[0].Enabled = false;
                        settingsDropdown.DropDownItems.Find("use_bridges_button", false)[0].Enabled = true;
                        settingsDropdown.DropDownItems.Find("bridges_list", false)[0].Enabled = false;
                        settingsDropdown.DropDownItems.Find("filter_running_button", false)[0].Enabled = false;
                        settingsDropdown.DropDownItems.Find("filter_update", false)[0].Enabled = false;
                        if (((ToolStripMenuItem)settingsDropdown.DropDownItems.Find("filter_running_button", false)[0]).Checked)
                        {
                            filterTimer.Start();
                        }
                        break;

                    case ProxyStatus.Starting:
                        notifyIcon.Icon = new Icon("images/icon_connecting.ico");
                        contextMenu.Items.Find("use_proxy_button", false)[0].Enabled = false;
                        ((ToolStripMenuItem)contextMenu.Items.Find("use_proxy_button", false)[0]).Checked = false;
                        TorService.EnableProxy(false);
                        contextMenu.Items.Find("disconnect_button", false)[0].Enabled = true;
                        contextMenu.Items.Find("connect_button", false)[0].Enabled = false;
                        settingsDropdown.DropDownItems.Find("reset_bridges_button", false)[0].Enabled = false;
                        settingsDropdown.DropDownItems.Find("use_bridges_button", false)[0].Enabled = false;
                        settingsDropdown.DropDownItems.Find("bridges_list", false)[0].Enabled = false;
                        settingsDropdown.DropDownItems.Find("filter_running_button", false)[0].Enabled = false;
                        settingsDropdown.DropDownItems.Find("filter_update", false)[0].Enabled = false;
                        if (((ToolStripMenuItem)settingsDropdown.DropDownItems.Find("filter_running_button", false)[0]).Checked)
                        {
                            filterTimer.Start();
                        }
                        break;

                    case ProxyStatus.Disabled:
                        notifyIcon.Icon = new Icon("images/icon_disconnected.ico");
                        contextMenu.Items.Find("use_proxy_button", false)[0].Enabled = false;
                        ((ToolStripMenuItem)contextMenu.Items.Find("use_proxy_button", false)[0]).Checked = false;
                        TorService.EnableProxy(false);
                        contextMenu.Items.Find("disconnect_button", false)[0].Enabled = false;
                        contextMenu.Items.Find("connect_button", false)[0].Enabled = true;
                        contextMenu.Items.Find("log_textbox", false)[0].Text = "NoProcess";
                        settingsDropdown.DropDownItems.Find("reset_bridges_button", false)[0].Enabled = true;
                        settingsDropdown.DropDownItems.Find("use_bridges_button", false)[0].Enabled = true;
                        settingsDropdown.DropDownItems.Find("bridges_list", false)[0].Enabled = true;
                        settingsDropdown.DropDownItems.Find("filter_running_button", false)[0].Enabled = true;
                        settingsDropdown.DropDownItems.Find("filter_update", false)[0].Enabled = true;
                        filterTimer.Stop();
                        break;
                }
            }));
            
        }

        private void TorService_OnStartupStatusChange(object sender, EventArgs e)
        {
            Invoke(new MethodInvoker(delegate { contextMenu.Items.Find("log_textbox", false)[0].Text = TorService.Instance.StartupStatus; }));
        }

        private void Settings_FormClosed(object sender, FormClosedEventArgs e)
        {
            TorService.Instance.StopTorProxy();
            TorService.Instance.WaitForEnd();
            TorService.EnableProxy(false);
            Application.Exit();
        }

        private void DisconnectButton_Click(object sender, EventArgs e)
        {
            TorService.EnableProxy(false);
            ((ToolStripMenuItem)contextMenu.Items.Find("use_proxy_button", false)[0]).Checked = false;

            TorService.Instance.StopTorProxy();
            TorService.Instance.WaitForEnd();
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            TorService.EnableProxy(false);
            ((ToolStripMenuItem)contextMenu.Items.Find("use_proxy_button", false)[0]).Checked = false;

            if (((ToolStripMenuItem)settingsDropdown.DropDownItems.Find("use_bridges_button", false)[0]).Checked)
            {
                string pulledBridges = settingsDropdown.DropDownItems.Find("bridges_list", false)[0].Text;
                using (HttpClient client = new HttpClient())
                {
                    if (settingsDropdown.DropDownItems.Find("bridges_list", false)[0].Text.StartsWith("http")) pulledBridges = client.Send(new HttpRequestMessage(HttpMethod.Get, settingsDropdown.DropDownItems.Find("bridges_list", false)[0].Text)).Content.ReadAsStringAsync().Result;
                }

                TorService.Instance.SetConfigurationValue("Bridge", pulledBridges.Split("\n").ToArray());
            }

            TorService.Instance.StartTorProxy();
        }

        private void UseProxyButton_Click(object sender, EventArgs e)
        {
            if (TorService.Instance.Status != ProxyStatus.Running)
            {
                ((ToolStripMenuItem)contextMenu.Items.Find("use_proxy_button", false)[0]).Checked = false;
                return;
            }
            TorService.EnableProxy(((ToolStripMenuItem)contextMenu.Items.Find("use_proxy_button", false)[0]).Checked);
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            TorService.EnableProxy(false);
            ((ToolStripMenuItem)contextMenu.Items.Find("use_proxy_button", false)[0]).Checked = false;
            if (TorService.DebugMode)
            {
                File.WriteAllLines(AppContext.BaseDirectory + "\\tor\\workingBridges.txt", TorService.Instance.WorkingBridges);
                File.WriteAllLines(AppContext.BaseDirectory + "\\tor\\filteredBridges.txt", TorService.Instance.FilteredBridges);
            }
            TorService.Instance.StopTorProxy();
            TorService.Instance.WaitForEnd();
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Name = "TorProxy";
            Text = "TorProxy";
            ResumeLayout(false);
        }

        #endregion
    }
}