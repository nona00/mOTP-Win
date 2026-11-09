// mOTP-Win - C# port of the original Visual Basic .NET application.
// Original VB.NET implementation by Lester Hightower, November 2011.
// The original source states that it is licensed under the GNU GPL version 3.0.

using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace mOTPWin;

public sealed class MainForm : Form
{
    private const string AppName = "mOTP-Win";
    private const string AppVersion = "1.0";

    private readonly TableLayoutPanel _mainPanel;
    private readonly TableLayoutPanel _configurationPanel;
    private readonly TableLayoutPanel _aboutPanel;

    private readonly TextBox _pinTextBox;
    private readonly ComboBox _profilesComboBox;
    private readonly TextBox _otpOutputTextBox;
    private readonly Label _otpCountdownLabel;
    private readonly ListBox _profilesListBox;
    private readonly TextBox _profileNameTextBox;
    private readonly Label _secretLabel;
    private readonly TextBox _secretTextBox;
    private readonly System.Windows.Forms.Timer _otpTimer;
    private string? _activePin;
    private string? _activeSecret;
    private long _activeOtpCounter = -1;

    public MainForm()
    {
        Text = "mOTP for .Net";
        ClientSize = new Size(250, 285);
        MinimumSize = new Size(266, 324);
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;

        // Create controls before building the panels, because the panel builders
        // assign the corresponding fields.
        _pinTextBox = new TextBox { PasswordChar = '*', Dock = DockStyle.Fill };
        _profilesComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill
        };
        _otpOutputTextBox = new TextBox
        {
            BackColor = SystemColors.Control,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            Text = "OTP will appear here...",
            TextAlign = HorizontalAlignment.Center,
            Font = new Font("Microsoft Sans Serif", 18F, FontStyle.Regular),
            Dock = DockStyle.Fill
        };
        _otpCountdownLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "New OTP in: --",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular),
        };
        _profilesListBox = new ListBox
        {
            MultiColumn = false,
            SelectionMode = SelectionMode.One,
            Dock = DockStyle.Fill,
            Height = 88
        };
        _profileNameTextBox = new TextBox { Dock = DockStyle.Fill };
        _secretLabel = new Label { AutoSize = true, Text = "Secret:" };
        _secretTextBox = new TextBox { Dock = DockStyle.Fill };

        Shown += (_, _) => _pinTextBox.Focus();

        _mainPanel = BuildMainPanel();
        _configurationPanel = BuildConfigurationPanel();
        _aboutPanel = BuildAboutPanel();
        _otpTimer = new System.Windows.Forms.Timer { Interval = 100 };
        _otpTimer.Tick += UpdateOtpCountdown;
        FormClosed += (_, _) =>
        {
            _otpTimer.Dispose();
            _activePin = null;
            _activeSecret = null;
        };

        var menu = BuildMenu();
        MainMenuStrip = menu;
        Controls.Add(_mainPanel);
        Controls.Add(_configurationPanel);
        Controls.Add(_aboutPanel);
        Controls.Add(menu);

        RefreshProfilesList();
        ShowPanel(_mainPanel);
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip();

        var fileMenu = new ToolStripMenuItem("File");
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Configuration", null, (_, _) =>
            ShowPanel(_configurationPanel)));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Quit", null, (_, _) => Close()));

        var helpMenu = new ToolStripMenuItem("Help");
        helpMenu.DropDownItems.Add(new ToolStripMenuItem("About", null, (_, _) =>
            ShowPanel(_aboutPanel)));

        menu.Items.Add(fileMenu);
        menu.Items.Add(helpMenu);
        return menu;
    }

    private TableLayoutPanel BuildMainPanel()
    {
        var panel = CreatePanel(columnCount: 2, rowCount: 7, padding: new Padding(6));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        //AddLabel(panel, "Get Password", 0, columnSpan: 2, centered: true, fontSize: 14F);
        AddLabel(panel, "Select Profile:", 1, columnSpan: 2);
        panel.Controls.Add(_profilesComboBox, 0, 2);
        panel.SetColumnSpan(_profilesComboBox, 2);
        AddLabel(panel, "Enter PIN and click OK:", 3, columnSpan: 2);

        panel.Controls.Add(_pinTextBox, 0, 4);
        var okButton = new Button
        {
            Text = "OK",
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            AutoSize = true
        };
        okButton.Click += GetPassword;
        _pinTextBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                okButton.PerformClick();
            }
        };
        panel.Controls.Add(okButton, 1, 4);

        panel.Controls.Add(_otpOutputTextBox, 0, 5);
        panel.SetColumnSpan(_otpOutputTextBox, 2);
        panel.Controls.Add(_otpCountdownLabel, 0, 6);
        panel.SetColumnSpan(_otpCountdownLabel, 2);
        panel.RowStyles[6] = new RowStyle(SizeType.Percent, 100F);
        return panel;
    }

    private TableLayoutPanel BuildConfigurationPanel()
    {
        var panel = CreatePanel(columnCount: 2, rowCount: 9, padding: new Padding(6));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        //AddLabel(panel, "Configuration", 0, columnSpan: 2, centered: true, fontSize: 14F);
        AddLabel(panel, "Profiles:", 1, columnSpan: 2);

        panel.Controls.Add(_profilesListBox, 0, 2);
        panel.SetColumnSpan(_profilesListBox, 2);
        _profilesListBox.SelectedValueChanged += ProfilesListBox_SelectedValueChanged;

        AddLabel(panel, "Profile Name:", 3, columnSpan: 2);
        panel.Controls.Add(_profileNameTextBox, 0, 4);
        panel.SetColumnSpan(_profileNameTextBox, 2);

        panel.Controls.Add(_secretLabel, 0, 5);
        panel.SetColumnSpan(_secretLabel, 2);
        panel.Controls.Add(_secretTextBox, 0, 6);
        panel.SetColumnSpan(_secretTextBox, 2);
        _secretTextBox.TextChanged += SecretTextBox_TextChanged;

        var deleteButton = new Button
        {
            Text = "Delete",
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            AutoSize = true
        };
        deleteButton.Click += DeleteProfile_Click;
        panel.Controls.Add(deleteButton, 0, 7);

        var saveButton = new Button
        {
            Text = "Save",
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            AutoSize = true
        };
        saveButton.Click += SaveProfile_Click;
        panel.Controls.Add(saveButton, 1, 7);

        var exitButton = new Button
        {
            Text = "Exit Configuration Screen",
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            AutoSize = true
        };
        exitButton.Click += (_, _) => ShowPanel(_mainPanel);
        panel.Controls.Add(exitButton, 0, 8);
        panel.SetColumnSpan(exitButton, 2);

        return panel;
    }

    private TableLayoutPanel BuildAboutPanel()
    {
        var panel = CreatePanel(columnCount: 1, rowCount: 3, padding: new Padding(6));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        var title = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = $"{AppName} v{AppVersion}",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Regular)
        };
        panel.Controls.Add(title, 0, 0);

        var aboutText = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Text = "A Mobile One Time Password Generator for .Net" + Environment.NewLine +
                   Environment.NewLine +
                   "Written by Lester Hightower" + Environment.NewLine +
                   "Licensed under the GPLv3" + Environment.NewLine +
                   Environment.NewLine +
                   "Learn more about MOTP at: http://motp.sourceforge.net/",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular)
        };
        panel.Controls.Add(aboutText, 0, 1);

        var okButton = new Button
        {
            Text = "OK",
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            AutoSize = true
        };
        okButton.Click += (_, _) => ShowPanel(_mainPanel);
        panel.Controls.Add(okButton, 0, 2);

        panel.RowStyles[0] = new RowStyle(SizeType.AutoSize);
        panel.RowStyles[1] = new RowStyle(SizeType.Percent, 100F);
        panel.RowStyles[2] = new RowStyle(SizeType.AutoSize);
        return panel;
    }

    private static TableLayoutPanel CreatePanel(int columnCount, int rowCount, Padding padding)
    {
        var panel = new TableLayoutPanel
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            ColumnCount = columnCount,
            RowCount = rowCount,
            Dock = DockStyle.Fill,
            Location = Point.Empty,
            Padding = padding,
            TabIndex = 0,
            Visible = false
        };

        // Most rows grow to fit their controls. Panel builders can replace a
        // row style with a percentage row to consume remaining vertical space.
        for (int row = 0; row < rowCount; row++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        return panel;
    }

    private static void AddLabel(
        TableLayoutPanel panel,
        string text,
        int row,
        int columnSpan = 1,
        bool centered = false,
        float fontSize = 9F)
    {
        var label = new Label
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            AutoSize = true,
            Font = new Font("Microsoft Sans Serif", fontSize, FontStyle.Regular),
            Text = text,
            TextAlign = centered ? ContentAlignment.MiddleCenter : ContentAlignment.BottomLeft
        };
        panel.Controls.Add(label, 0, row);
        if (columnSpan > 1)
        {
            panel.SetColumnSpan(label, columnSpan);
        }
    }

    private void ShowPanel(TableLayoutPanel selectedPanel)
    {
        _mainPanel.Visible = false;
        _configurationPanel.Visible = false;
        _aboutPanel.Visible = false;
        selectedPanel.Visible = true;
        selectedPanel.BringToFront();
        MainMenuStrip?.BringToFront();
    }

    private void GetPassword(object? sender, EventArgs e)
    {
        string chosenProfile = _profilesComboBox.SelectedItem?.ToString() ?? string.Empty;
        string pin = _pinTextBox.Text;
        IniFile? profiles = GetProfiles();

        if (profiles is null)
        {
            _otpTimer.Stop();
            _activePin = null;
            _activeSecret = null;
            _otpOutputTextBox.Text = "No Profiles Configured!";
            _otpCountdownLabel.Text = "New OTP in: --";
            return;
        }

        string secret = profiles.GetKeyValue(chosenProfile, "secret");
        _activePin = pin;
        _activeSecret = secret;
        GenerateOtp(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 10);
        UpdateOtpCountdown(this, EventArgs.Empty);
        _otpTimer.Start();
        _pinTextBox.Clear(); // Clear the PIN after use, as in the VB.NET version.
        _pinTextBox.Focus();
    }

    private void UpdateOtpCountdown(object? sender, EventArgs e)
    {
        long unixTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long currentCounter = unixTimeMilliseconds / 10000;

        if (currentCounter != _activeOtpCounter)
        {
            GenerateOtp(currentCounter);
        }

        long secondsRemaining = 10 - (unixTimeMilliseconds / 1000 % 10);
        _otpCountdownLabel.Text = $"New OTP in: {secondsRemaining} s";
    }

    private void GenerateOtp(long counter)
    {
        if (_activePin is null || _activeSecret is null)
        {
            return;
        }

        string authKey = counter.ToString(CultureInfo.InvariantCulture) + _activeSecret + _activePin;
        _otpOutputTextBox.Text = CalculateMd5Hash(authKey).Substring(0, 6);
        _activeOtpCounter = counter;
    }

    private static string CalculateMd5Hash(string value)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
            var result = new StringBuilder(hash.Length * 2);
            foreach (byte item in hash)
            {
                result.Append(item.ToString("x2", CultureInfo.InvariantCulture));
            }

            return result.ToString();
        }
    }

    private void RefreshProfilesList(string? preferredProfile = null)
    {
        string[] profiles = GetProfileNames();

        _profilesComboBox.BeginUpdate();
        _profilesComboBox.Items.Clear();
        _profilesComboBox.Items.AddRange(profiles);
        _profilesComboBox.EndUpdate();

        _profilesListBox.BeginUpdate();
        _profilesListBox.Items.Clear();
        _profilesListBox.Items.AddRange(profiles);
        _profilesListBox.EndUpdate();

        if (profiles.Length == 0)
        {
            _profileNameTextBox.Clear();
            _secretTextBox.Clear();
            return;
        }

        int preferredIndex = preferredProfile is null
            ? -1
            : Array.FindIndex(profiles, name =>
                string.Equals(name, preferredProfile, StringComparison.OrdinalIgnoreCase));
        int selectedIndex = preferredIndex >= 0 ? preferredIndex : 0;

        _profilesComboBox.SelectedIndex = selectedIndex;
        _profilesListBox.SelectedIndex = selectedIndex;
        _profilesListBox.TopIndex = selectedIndex;
    }

    private static string[] GetProfileNames()
    {
        IniFile? profiles = GetProfiles();
        if (profiles is null)
        {
            return Array.Empty<string>();
        }

        return profiles.SectionNames
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string GetIniFilename()
    {
        string executablePath = Application.ExecutablePath;
        string? iniPath = Path.ChangeExtension(executablePath, ".ini");
        return iniPath ?? Path.Combine(AppContext.BaseDirectory, $"{AppName}.ini");
    }

    private static IniFile? GetProfiles()
    {
        string iniFilename = GetIniFilename();
        if (!File.Exists(iniFilename))
        {
            return null;
        }

        try
        {
            var ini = new IniFile();
            ini.Load(iniFilename);
            return ini;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                                   ex is FormatException)
        {
            return null;
        }
    }

    private void DeleteProfile_Click(object? sender, EventArgs e)
    {
        string? chosenProfile = _profilesListBox.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(chosenProfile))
        {
            return;
        }

        DialogResult result = MessageBox.Show(
            this,
            "Are you sure that you wish to delete" + Environment.NewLine +
            $"the profile named \"{chosenProfile}\"?" + Environment.NewLine +
            Environment.NewLine +
            "This action cannot be undone!",
            "Delete a Profile?",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            return;
        }

        IniFile profiles = GetProfiles() ?? new IniFile();
        profiles.RemoveSection(chosenProfile);
        profiles.Save(GetIniFilename());
        RefreshProfilesList();
    }

    private void SaveProfile_Click(object? sender, EventArgs e)
    {
        string profileName = _profileNameTextBox.Text.Trim();
        if (profileName.Length == 0)
        {
            return;
        }

        IniFile profiles = GetProfiles() ?? new IniFile();
        profiles.SetKeyValue(profileName, "secret", _secretTextBox.Text);
        profiles.Save(GetIniFilename());
        RefreshProfilesList(profileName);
    }

    private void SecretTextBox_TextChanged(object? sender, EventArgs e)
    {
        _secretLabel.Text = $"Secret ({_secretTextBox.TextLength} chars):";
    }

    private void ProfilesListBox_SelectedValueChanged(object? sender, EventArgs e)
    {
        if (_profilesListBox.SelectedIndex < 0 ||
            _profilesListBox.SelectedItem is not string profileName)
        {
            return;
        }

        _profileNameTextBox.Text = profileName;
        IniFile? profiles = GetProfiles();
        _secretTextBox.Text = profiles?.GetKeyValue(profileName, "secret") ?? string.Empty;
    }
}
