using System.Linq.Expressions;
using TorProxy.Proxy;

namespace TorProxy
{
    public partial class IconUserInterface : Form
    {

        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;

        public IconUserInterface()
        {
            Name = "TorProxy";
            Text = "TorProxy";

            TorService.Instance.OnStartupStatusChange += TorService_OnStartupStatusChange;
            TorService.Instance.OnStatusChange += TorService_OnStatusChange;

            InitializeComponent();

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
                        Enabled = false,
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
                        Enabled = false,
                    },
                    new ToolStripTextBox()
                    {
                        Name = "log_textbox",
                        AcceptsTab = false,
                        Enabled = false,
                        Text = "NoProcess",
                    },
                    new ToolStripButton()
                    {
                        Name = "settings_button",
                        Enabled = true,
                        Text = "Settings",
                    },
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
            contextMenu.Items.Find("settings_button", false)[0].Click += OpenSettingsButton_Click;
            Size = new Size(0, 0);
            Visible = false;
            ShowInTaskbar = false;
            Hide();
            WindowState = FormWindowState.Minimized;
            FormClosed += Settings_FormClosed;
        }

        private void OpenSettingsButton_Click(object? sender, EventArgs e)
        {
            SettingsWindow.Instance.Invoke(() =>
            {
                if (SettingsWindow.Instance.WindowState == FormWindowState.Minimized)
                {
                    SettingsWindow.Instance.WindowState = FormWindowState.Normal;
                    SettingsWindow.Instance.Invoke(() => SettingsWindow.Instance.Visible = true);
                }
                else
                {
                    SettingsWindow.Instance.Invoke(() => SettingsWindow.Instance.Visible = false);
                    SettingsWindow.Instance.WindowState = FormWindowState.Minimized;
                }
            });
        }

        private void TorService_OnStatusChange(object sender, EventArgs e)
        {
            Invoke(() =>
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
                        break;

                    case ProxyStatus.Starting:
                        notifyIcon.Icon = new Icon("images/icon_connecting.ico");
                        contextMenu.Items.Find("use_proxy_button", false)[0].Enabled = false;
                        ((ToolStripMenuItem)contextMenu.Items.Find("use_proxy_button", false)[0]).Checked = false;
                        TorService.EnableProxy(false);
                        contextMenu.Items.Find("disconnect_button", false)[0].Enabled = true;
                        contextMenu.Items.Find("connect_button", false)[0].Enabled = false;
                        break;

                    case ProxyStatus.Disabled:
                        notifyIcon.Icon = new Icon("images/icon_disconnected.ico");
                        contextMenu.Items.Find("use_proxy_button", false)[0].Enabled = false;
                        ((ToolStripMenuItem)contextMenu.Items.Find("use_proxy_button", false)[0]).Checked = false;
                        TorService.EnableProxy(false);
                        contextMenu.Items.Find("disconnect_button", false)[0].Enabled = false;
                        contextMenu.Items.Find("connect_button", false)[0].Enabled = true;
                        contextMenu.Items.Find("log_textbox", false)[0].Text = "NoProcess";
                        break;
                }
            });
        }

        private void TorService_OnStartupStatusChange(object sender, EventArgs e)
        {
            Invoke(() => 
            {
                contextMenu.Items.Find("log_textbox", false)[0].Text = TorService.Instance.StartupStatus;
            });
        }

        private void Settings_FormClosed(object sender, FormClosedEventArgs e)
        {
            TorService.EnableProxy(false);
            TorService.Instance.StopTorProxy();
            TorService.Instance.WaitForEnd();
            Application.Exit();
        }

        private void DisconnectButton_Click(object sender, EventArgs e)
        {
            if (!TorService.Instance.ProxyRunning) return;

            ((ToolStripMenuItem)contextMenu.Items.Find("use_proxy_button", false)[0]).Checked = false;
            TorService.EnableProxy(false);
            SettingsWindow.Instance.Invoke(() =>
            {
                SettingsWindow.Instance.disconnect_button.PerformClick();
            });
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)contextMenu.Items.Find("use_proxy_button", false)[0]).Checked = false;
            TorService.EnableProxy(false);
            SettingsWindow.Instance.Invoke(() =>
            {
                SettingsWindow.Instance.connect_button.PerformClick();
            });
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
            ((ToolStripMenuItem)contextMenu.Items.Find("use_proxy_button", false)[0]).Checked = false;
            TorService.Instance.StopTorProxy();
            TorService.Instance.WaitForEnd();
            Application.Exit();
        }
    }
}