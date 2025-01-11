namespace TorProxy
{
    partial class SettingsWindow
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
            TextBox bridges_count_textbox_label;
            TextBox filter_reload_time_textbox_label;
            current_bridges_list = new ListBox();
            use_bridges_checkbox = new CheckBox();
            reset_bridges_button = new Button();
            bridges_list_textbox = new TextBox();
            bridge_type_combobox = new ComboBox();
            bridges_control_panel = new Panel();
            filter_reload_time_textbox = new NumericUpDown();
            bridges_count_textbox = new NumericUpDown();
            connect_button = new Button();
            disconnect_button = new Button();
            bridges_ration_label = new Label();
            unfiltered_bridges_count_label = new Label();
            total_bridges_count_label = new Label();
            error_label = new Label();
            status_label = new Label();
            log_textbox = new TextBox();
            copy_all_button = new Button();
            copy_obfs4_button = new Button();
            copy_webtunnel = new Button();
            bridges_count_textbox_label = new TextBox();
            filter_reload_time_textbox_label = new TextBox();
            bridges_control_panel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)filter_reload_time_textbox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)bridges_count_textbox).BeginInit();
            SuspendLayout();
            // 
            // bridges_count_textbox_label
            // 
            bridges_count_textbox_label.BackColor = SystemColors.Window;
            bridges_count_textbox_label.Location = new Point(12, 240);
            bridges_count_textbox_label.Name = "bridges_count_textbox_label";
            bridges_count_textbox_label.ReadOnly = true;
            bridges_count_textbox_label.ShortcutsEnabled = false;
            bridges_count_textbox_label.Size = new Size(217, 27);
            bridges_count_textbox_label.TabIndex = 6;
            bridges_count_textbox_label.TabStop = false;
            bridges_count_textbox_label.Text = "Running bridges count:";
            bridges_count_textbox_label.WordWrap = false;
            // 
            // filter_reload_time_textbox_label
            // 
            filter_reload_time_textbox_label.BackColor = SystemColors.Window;
            filter_reload_time_textbox_label.HideSelection = false;
            filter_reload_time_textbox_label.ImeMode = ImeMode.NoControl;
            filter_reload_time_textbox_label.Location = new Point(12, 266);
            filter_reload_time_textbox_label.Name = "filter_reload_time_textbox_label";
            filter_reload_time_textbox_label.ReadOnly = true;
            filter_reload_time_textbox_label.ShortcutsEnabled = false;
            filter_reload_time_textbox_label.Size = new Size(217, 27);
            filter_reload_time_textbox_label.TabIndex = 8;
            filter_reload_time_textbox_label.TabStop = false;
            filter_reload_time_textbox_label.Text = "Filter reload time:";
            filter_reload_time_textbox_label.WordWrap = false;
            // 
            // current_bridges_list
            // 
            current_bridges_list.AccessibleDescription = "List of active and inactive bridges used";
            current_bridges_list.AccessibleName = "List of bridges";
            current_bridges_list.AccessibleRole = AccessibleRole.None;
            current_bridges_list.FormattingEnabled = true;
            current_bridges_list.HorizontalScrollbar = true;
            current_bridges_list.ItemHeight = 20;
            current_bridges_list.Location = new Point(376, 17);
            current_bridges_list.Name = "current_bridges_list";
            current_bridges_list.Size = new Size(353, 584);
            current_bridges_list.TabIndex = 0;
            current_bridges_list.KeyDown += current_bridges_list_KeyDown;
            // 
            // use_bridges_checkbox
            // 
            use_bridges_checkbox.AutoSize = true;
            use_bridges_checkbox.BackColor = SystemColors.ControlLight;
            use_bridges_checkbox.BackgroundImageLayout = ImageLayout.None;
            use_bridges_checkbox.FlatAppearance.BorderSize = 0;
            use_bridges_checkbox.Location = new Point(15, 18);
            use_bridges_checkbox.Name = "use_bridges_checkbox";
            use_bridges_checkbox.Size = new Size(116, 24);
            use_bridges_checkbox.TabIndex = 2;
            use_bridges_checkbox.Text = "Use bridges?";
            use_bridges_checkbox.UseVisualStyleBackColor = false;
            // 
            // reset_bridges_button
            // 
            reset_bridges_button.Location = new Point(236, 18);
            reset_bridges_button.Name = "reset_bridges_button";
            reset_bridges_button.Size = new Size(133, 29);
            reset_bridges_button.TabIndex = 3;
            reset_bridges_button.Text = "Reset bridges";
            reset_bridges_button.UseVisualStyleBackColor = true;
            reset_bridges_button.Click += reset_bridges_button_Click;
            // 
            // bridges_list_textbox
            // 
            bridges_list_textbox.Location = new Point(12, 53);
            bridges_list_textbox.Multiline = true;
            bridges_list_textbox.Name = "bridges_list_textbox";
            bridges_list_textbox.Size = new Size(358, 162);
            bridges_list_textbox.TabIndex = 4;
            bridges_list_textbox.WordWrap = false;
            // 
            // bridge_type_combobox
            // 
            bridge_type_combobox.BackColor = SystemColors.Window;
            bridge_type_combobox.FormattingEnabled = true;
            bridge_type_combobox.Items.AddRange(new object[] { "obfs4", "webtunnel", "obfs4 + webtunnel", "any" });
            bridge_type_combobox.Location = new Point(12, 213);
            bridge_type_combobox.Name = "bridge_type_combobox";
            bridge_type_combobox.Size = new Size(358, 28);
            bridge_type_combobox.TabIndex = 5;
            bridge_type_combobox.KeyPress += bridge_type_combobox_KeyPress;
            // 
            // bridges_control_panel
            // 
            bridges_control_panel.BackColor = SystemColors.ControlLight;
            bridges_control_panel.Controls.Add(filter_reload_time_textbox);
            bridges_control_panel.Controls.Add(bridges_count_textbox);
            bridges_control_panel.Location = new Point(12, 17);
            bridges_control_panel.Name = "bridges_control_panel";
            bridges_control_panel.Size = new Size(358, 276);
            bridges_control_panel.TabIndex = 10;
            // 
            // filter_reload_time_textbox
            // 
            filter_reload_time_textbox.Location = new Point(216, 249);
            filter_reload_time_textbox.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            filter_reload_time_textbox.Name = "filter_reload_time_textbox";
            filter_reload_time_textbox.RightToLeft = RightToLeft.No;
            filter_reload_time_textbox.Size = new Size(142, 27);
            filter_reload_time_textbox.TabIndex = 13;
            filter_reload_time_textbox.TextAlign = HorizontalAlignment.Right;
            filter_reload_time_textbox.Value = new decimal(new int[] { 11, 0, 0, 0 });
            // 
            // bridges_count_textbox
            // 
            bridges_count_textbox.Location = new Point(216, 223);
            bridges_count_textbox.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            bridges_count_textbox.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            bridges_count_textbox.Name = "bridges_count_textbox";
            bridges_count_textbox.Size = new Size(142, 27);
            bridges_count_textbox.TabIndex = 0;
            bridges_count_textbox.TextAlign = HorizontalAlignment.Right;
            bridges_count_textbox.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // connect_button
            // 
            connect_button.Location = new Point(275, 632);
            connect_button.Name = "connect_button";
            connect_button.Size = new Size(94, 29);
            connect_button.TabIndex = 11;
            connect_button.Text = "Connect";
            connect_button.UseVisualStyleBackColor = true;
            connect_button.Click += connect_button_Click;
            // 
            // disconnect_button
            // 
            disconnect_button.Enabled = false;
            disconnect_button.Location = new Point(12, 632);
            disconnect_button.Name = "disconnect_button";
            disconnect_button.Size = new Size(94, 29);
            disconnect_button.TabIndex = 12;
            disconnect_button.Text = "Disconnect";
            disconnect_button.UseVisualStyleBackColor = true;
            disconnect_button.Click += disconnect_button_Click;
            // 
            // bridges_ration_label
            // 
            bridges_ration_label.AutoSize = true;
            bridges_ration_label.Location = new Point(12, 296);
            bridges_ration_label.Name = "bridges_ration_label";
            bridges_ration_label.Size = new Size(202, 20);
            bridges_ration_label.TabIndex = 13;
            bridges_ration_label.Text = "Working/Dead bridges ratio: ";
            // 
            // unfiltered_bridges_count_label
            // 
            unfiltered_bridges_count_label.AutoSize = true;
            unfiltered_bridges_count_label.Location = new Point(12, 316);
            unfiltered_bridges_count_label.Name = "unfiltered_bridges_count_label";
            unfiltered_bridges_count_label.Size = new Size(132, 20);
            unfiltered_bridges_count_label.TabIndex = 14;
            unfiltered_bridges_count_label.Text = "Unfiltered bridges:";
            // 
            // total_bridges_count_label
            // 
            total_bridges_count_label.AutoSize = true;
            total_bridges_count_label.Location = new Point(12, 336);
            total_bridges_count_label.Name = "total_bridges_count_label";
            total_bridges_count_label.Size = new Size(146, 20);
            total_bridges_count_label.TabIndex = 15;
            total_bridges_count_label.Text = "Total bridges added:";
            total_bridges_count_label.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // error_label
            // 
            error_label.AutoSize = true;
            error_label.ForeColor = Color.Red;
            error_label.Location = new Point(15, 609);
            error_label.Name = "error_label";
            error_label.Size = new Size(0, 20);
            error_label.TabIndex = 16;
            error_label.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // status_label
            // 
            status_label.AutoSize = true;
            status_label.Location = new Point(12, 356);
            status_label.Name = "status_label";
            status_label.Size = new Size(52, 20);
            status_label.TabIndex = 17;
            status_label.Text = "Status:";
            // 
            // log_textbox
            // 
            log_textbox.BackColor = SystemColors.Window;
            log_textbox.Location = new Point(12, 379);
            log_textbox.Multiline = true;
            log_textbox.Name = "log_textbox";
            log_textbox.ReadOnly = true;
            log_textbox.ScrollBars = ScrollBars.Both;
            log_textbox.Size = new Size(358, 222);
            log_textbox.TabIndex = 18;
            log_textbox.WordWrap = false;
            // 
            // copy_all_button
            // 
            copy_all_button.Location = new Point(376, 632);
            copy_all_button.Name = "copy_all_button";
            copy_all_button.Size = new Size(94, 29);
            copy_all_button.TabIndex = 19;
            copy_all_button.Text = "Copy all";
            copy_all_button.UseVisualStyleBackColor = true;
            copy_all_button.Click += copy_all_button_Click;
            // 
            // copy_obfs4_button
            // 
            copy_obfs4_button.Location = new Point(476, 632);
            copy_obfs4_button.Name = "copy_obfs4_button";
            copy_obfs4_button.Size = new Size(100, 29);
            copy_obfs4_button.TabIndex = 20;
            copy_obfs4_button.Text = "Copy obfs4";
            copy_obfs4_button.UseVisualStyleBackColor = true;
            copy_obfs4_button.Click += copy_obfs4_button_Click;
            // 
            // copy_webtunnel
            // 
            copy_webtunnel.Location = new Point(582, 632);
            copy_webtunnel.Name = "copy_webtunnel";
            copy_webtunnel.Size = new Size(147, 29);
            copy_webtunnel.TabIndex = 21;
            copy_webtunnel.Text = "Copy webtunnel";
            copy_webtunnel.UseVisualStyleBackColor = true;
            copy_webtunnel.Click += copy_webtunnel_Click;
            // 
            // SettingsWindow
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(741, 672);
            Controls.Add(copy_webtunnel);
            Controls.Add(copy_obfs4_button);
            Controls.Add(copy_all_button);
            Controls.Add(log_textbox);
            Controls.Add(status_label);
            Controls.Add(error_label);
            Controls.Add(total_bridges_count_label);
            Controls.Add(unfiltered_bridges_count_label);
            Controls.Add(bridges_ration_label);
            Controls.Add(bridges_count_textbox_label);
            Controls.Add(disconnect_button);
            Controls.Add(connect_button);
            Controls.Add(filter_reload_time_textbox_label);
            Controls.Add(bridge_type_combobox);
            Controls.Add(bridges_list_textbox);
            Controls.Add(current_bridges_list);
            Controls.Add(reset_bridges_button);
            Controls.Add(use_bridges_checkbox);
            Controls.Add(bridges_control_panel);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MdiChildrenMinimizedAnchorBottom = false;
            Name = "SettingsWindow";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "TorProxy Settings";
            bridges_control_panel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)filter_reload_time_textbox).EndInit();
            ((System.ComponentModel.ISupportInitialize)bridges_count_textbox).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListBox current_bridges_list;
        private TextBox bridges_list_textbox;
        private ComboBox bridge_type_combobox;
        private TextBox bridges_count_textbox_label;
        private TextBox filter_reload_time_textbox_label;
        private Panel bridges_control_panel;
        private NumericUpDown bridges_count_textbox;
        private NumericUpDown filter_reload_time_textbox;
        private Label bridges_ration_label;
        private Label unfiltered_bridges_count_label;
        private Label total_bridges_count_label;
        private Label error_label;
        private Label status_label;
        private TextBox log_textbox;
        public Button connect_button;
        public Button disconnect_button;
        public CheckBox use_bridges_checkbox;
        public Button reset_bridges_button;
        private Button copy_all_button;
        private Button copy_obfs4_button;
        private Button copy_webtunnel;
    }
}