namespace TorProxy.GUI
{
    partial class Settings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Settings));
            use_dns_checkbox = new CheckBox();
            filter_type_combobox = new ComboBox();
            proxifyre_apps = new ListBox();
            proxifyre_remove_button = new Button();
            use_as_proxy_checkbox = new CheckBox();
            proxifyre_add_button = new Button();
            SuspendLayout();
            // 
            // use_dns_checkbox
            // 
            use_dns_checkbox.AutoSize = true;
            use_dns_checkbox.Location = new Point(12, 12);
            use_dns_checkbox.Name = "use_dns_checkbox";
            use_dns_checkbox.RightToLeft = RightToLeft.Yes;
            use_dns_checkbox.Size = new Size(112, 24);
            use_dns_checkbox.TabIndex = 0;
            use_dns_checkbox.Text = "?Use tor dns";
            use_dns_checkbox.UseVisualStyleBackColor = true;
            use_dns_checkbox.CheckedChanged += use_dns_checkbox_CheckedChanged;
            // 
            // filter_type_combobox
            // 
            filter_type_combobox.DropDownStyle = ComboBoxStyle.DropDownList;
            filter_type_combobox.FormattingEnabled = true;
            filter_type_combobox.Items.AddRange(new object[] { "None (Network monitor)", "Network Blocking filter", "Proxy Tunneler", "ProxiFyre (Select apps)" });
            filter_type_combobox.Location = new Point(12, 72);
            filter_type_combobox.Name = "filter_type_combobox";
            filter_type_combobox.Size = new Size(207, 28);
            filter_type_combobox.TabIndex = 1;
            filter_type_combobox.TextChanged += filter_type_combobox_TextChanged;
            // 
            // proxifyre_apps
            // 
            proxifyre_apps.FormattingEnabled = true;
            proxifyre_apps.ItemHeight = 20;
            proxifyre_apps.Location = new Point(12, 106);
            proxifyre_apps.Name = "proxifyre_apps";
            proxifyre_apps.Size = new Size(207, 124);
            proxifyre_apps.TabIndex = 2;
            // 
            // proxifyre_remove_button
            // 
            proxifyre_remove_button.Location = new Point(125, 236);
            proxifyre_remove_button.Name = "proxifyre_remove_button";
            proxifyre_remove_button.Size = new Size(94, 29);
            proxifyre_remove_button.TabIndex = 4;
            proxifyre_remove_button.Text = "Remove";
            proxifyre_remove_button.UseVisualStyleBackColor = true;
            proxifyre_remove_button.Click += proxifyre_remove_button_Click;
            // 
            // use_as_proxy_checkbox
            // 
            use_as_proxy_checkbox.AutoSize = true;
            use_as_proxy_checkbox.Location = new Point(12, 42);
            use_as_proxy_checkbox.Name = "use_as_proxy_checkbox";
            use_as_proxy_checkbox.RightToLeft = RightToLeft.Yes;
            use_as_proxy_checkbox.Size = new Size(193, 24);
            use_as_proxy_checkbox.TabIndex = 5;
            use_as_proxy_checkbox.Text = "?Use tor as system proxy";
            use_as_proxy_checkbox.UseVisualStyleBackColor = true;
            use_as_proxy_checkbox.CheckedChanged += use_as_proxy_checkbox_CheckedChanged;
            // 
            // proxifyre_add_button
            // 
            proxifyre_add_button.Location = new Point(12, 236);
            proxifyre_add_button.Name = "proxifyre_add_button";
            proxifyre_add_button.Size = new Size(94, 29);
            proxifyre_add_button.TabIndex = 3;
            proxifyre_add_button.Text = "Add";
            proxifyre_add_button.UseVisualStyleBackColor = true;
            proxifyre_add_button.Click += proxifyre_add_button_Click;
            // 
            // Settings
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(232, 275);
            Controls.Add(use_as_proxy_checkbox);
            Controls.Add(proxifyre_remove_button);
            Controls.Add(proxifyre_add_button);
            Controls.Add(proxifyre_apps);
            Controls.Add(filter_type_combobox);
            Controls.Add(use_dns_checkbox);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Settings";
            ShowInTaskbar = false;
            Text = "Settings";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private CheckBox use_dns_checkbox;
        private ComboBox filter_type_combobox;
        private ListBox proxifyre_apps;
        private Button proxifyre_remove_button;
        private CheckBox use_as_proxy_checkbox;
        private Button proxifyre_add_button;
    }
}