using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TorProxy.Listener;
using TorProxy.Proxy;

namespace TorProxy.GUI
{
    public partial class Settings : Form
    {

        public static Settings Instance;

        public Settings()
        {
            InitializeComponent();
            Instance = this;
            WindowState = FormWindowState.Minimized;
            MaximizeBox = false;
            //ControlBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = true;
            filter_type_combobox.SelectedIndex = Convert.ToInt32(Configuration.Instance.Get("NetworkFilterType").First());
            use_dns_checkbox.Checked = Configuration.Instance.Get("UseTorDNS").First() == "1";
            use_as_proxy_checkbox.Checked = Configuration.Instance.Get("UseTorAsSystemProxy").First() == "1";
            proxifyre_apps.Items.AddRange(Configuration.Instance.Get("ProxiFyreApps"));
            FormClosing += Settings_FormClosing;
        }

        private void Settings_FormClosing(object? sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Instance.Visible = false;
            Instance.WindowState = FormWindowState.Minimized;
        }

        private void use_dns_checkbox_CheckedChanged(object sender, EventArgs e)
        {
            if (!Utils.IsAdministrator())
            {
                use_dns_checkbox.Checked = false;
                return;
            }
            Configuration.Instance.Set("UseTorDNS", new string[] { use_dns_checkbox.Checked ? "1" : "0" });
            if (TorServiceListener.IsEnabled) ReloadTor();
        }

        private void use_as_proxy_checkbox_CheckedChanged(object sender, EventArgs e)
        {
            if (filter_type_combobox.SelectedIndex == 3 && use_as_proxy_checkbox.Checked) use_as_proxy_checkbox.Checked = false;
            Configuration.Instance.Set("UseTorAsSystemProxy", new string[] { use_as_proxy_checkbox.Checked ? "1" : "0" });
            if (TorServiceListener.IsEnabled) ReloadTor();
        }

        private void filter_type_combobox_TextChanged(object sender, EventArgs e)
        {
            if (filter_type_combobox.SelectedIndex == 2)
            {
                filter_type_combobox.SelectedIndex = 0; // Since proxytunneler is unfinished, hardcoded skip
                return;
            }

            if (filter_type_combobox.SelectedIndex == 3 && use_as_proxy_checkbox.Checked) use_as_proxy_checkbox.Checked = false;

            Configuration.Instance.Set("NetworkFilterType", new string[] { filter_type_combobox.SelectedIndex.ToString() }); 
            if (TorServiceListener.IsEnabled) ReloadTor();
        }

        private void proxifyre_add_button_Click(object sender, EventArgs e)
        {
            var filePath = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "exe files (*.exe)|*.exe";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;

                    var fileStream = openFileDialog.OpenFile();
                }
            }
            proxifyre_apps.Items.Add(filePath);
            UpdateProxiFyreList();
        }

        private void proxifyre_remove_button_Click(object sender, EventArgs e)
        {
            if (proxifyre_apps.Items.Count == 0) return;
            if (proxifyre_apps.SelectedIndex == -1) return;
            proxifyre_apps.Items.RemoveAt(proxifyre_apps.SelectedIndex);
            UpdateProxiFyreList();
        }

        private void ReloadTor()
        {
            TorServiceListener.EnableTor(false);
            TorServiceListener.Firewall?.Stop();
            TorServiceListener.Firewall?.Dispose();
            TorServiceListener.InitializeFirewall();
            TorServiceListener.EnableTor(true);
        }

        private void UpdateProxiFyreList()
        {
            Configuration.Instance.Set("ProxiFyreApps", proxifyre_apps.Items.Cast<string>().ToArray());
            if (TorServiceListener.IsEnabled) ReloadTor();
        }
    }
}
