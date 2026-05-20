using System.Windows.Forms;

namespace PosNetworkSetup;

/// <summary>
/// Writes appsettings.Local.json, app-runtime-config.json, and CORS in appsettings.Development.json from one host + ports.
/// </summary>
public sealed class MainForm : Form
{
    private readonly TextBox _repoRoot = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Width = 420 };
    private readonly TextBox _host = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Width = 200 };
    private readonly NumericUpDown _angularPort = new() { Minimum = 1, Maximum = 65535, Value = 4200, Width = 80 };
    private readonly NumericUpDown _httpApiPort = new() { Minimum = 1, Maximum = 65535, Value = 5000, Width = 80 };
    private readonly NumericUpDown _httpsApiPort = new() { Minimum = 1, Maximum = 65535, Value = 5001, Width = 80 };
    private readonly NumericUpDown _imagePort = new() { Minimum = 1, Maximum = 65535, Value = 9096, Width = 80 };
    private readonly Label _status = new() { AutoSize = true, MaximumSize = new Size(520, 0) };

    private static readonly string LastRepoPathFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OpenSourcePosSetup",
        "last_repo_root.txt");

    public MainForm()
    {
        Text = "POS — LAN / VPN network setup";
        Width = 580;
        Height = 400;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            Padding = new Padding(12),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int r = 0;
        AddRow(panel, r++, "Repository root", RepoRow());
        AddRow(panel, r++, "Host / IP / PC name", HostRow());
        AddRow(panel, r++, "Angular port", _angularPort);
        AddRow(panel, r++, "API HTTP port", _httpApiPort);
        AddRow(panel, r++, "API HTTPS port", _httpsApiPort);
        AddRow(panel, r++, "Image server port", _imagePort);

        var filesLabel = new Label
        {
            Text = "Save updates:\r\n• open-source-pos/appsettings.Local.json\r\n• open-source-pos/appsettings.Development.json (CORS)\r\n• open-source-pos-frontend/src/assets/app-runtime-config.json",
            AutoSize = true,
            MaximumSize = new Size(400, 0),
        };
        panel.Controls.Add(filesLabel, 0, r);
        panel.SetColumnSpan(filesLabel, 2);
        r++;

        var save = new Button { Text = "Save all", Width = 120, Height = 32 };
        save.Click += (_, _) => SaveAll();

        var load = new Button { Text = "Load saved", Width = 100, Height = 32 };
        load.Click += (_, _) => LoadFromRepo();

        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(12) };
        footer.Controls.Add(save);
        footer.Controls.Add(load);
        footer.Controls.Add(_status);

        Controls.Add(footer);
        Controls.Add(panel);

        Load += (_, _) =>
        {
            TryLoadLastRepo();
            LoadFromRepo();
        };

        static void AddRow(TableLayoutPanel p, int row, string label, Control valueControl)
        {
            p.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            var wrap = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, Dock = DockStyle.Fill };
            wrap.Controls.Add(valueControl);
            p.Controls.Add(wrap, 1, row);
        }
    }

    private Control RepoRow()
    {
        var browse = new Button { Text = "Browse…", AutoSize = true };
        browse.Click += (_, _) =>
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Select repository root (folder containing open-source-pos and open-source-pos-frontend)",
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _repoRoot.Text = dlg.SelectedPath;
                LoadFromRepo();
            }
        };
        var flow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true };
        flow.Controls.Add(_repoRoot);
        flow.Controls.Add(browse);
        return flow;
    }

    private Control HostRow()
    {
        var detect = new Button { Text = "Detect IP", AutoSize = true };
        detect.Click += (_, _) =>
        {
            var ip = NetworkConfigWriter.TryDetectLanIPv4();
            if (ip != null)
            {
                _host.Text = ip;
                _status.Text = $"Detected IPv4: {ip}";
            }
            else
                MessageBox.Show(this, "Could not detect an active IPv4 address. Enter IP manually (e.g. 10.0.157.138).", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        var flow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true };
        flow.Controls.Add(_host);
        flow.Controls.Add(detect);
        return flow;
    }

    private void TryLoadLastRepo()
    {
        try
        {
            if (File.Exists(LastRepoPathFile))
                _repoRoot.Text = File.ReadAllText(LastRepoPathFile).Trim();
        }
        catch
        {
            // ignore
        }
    }

    private void LoadFromRepo()
    {
        _status.Text = string.Empty;
        var root = _repoRoot.Text.Trim();
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            return;

        var saved = NetworkConfigWriter.TryLoadFromRepo(root);
        if (saved == null)
        {
            _status.Text = "No appsettings.Local.json yet — enter host and Save all.";
            return;
        }

        _host.Text = saved.Host;
        _angularPort.Value = saved.AngularPort;
        _httpApiPort.Value = saved.HttpApiPort;
        _httpsApiPort.Value = saved.HttpsApiPort;
        _imagePort.Value = saved.ImagePort;
        _status.Text = $"Loaded from appsettings.Local.json ({saved.Host}).";
    }

    private void SaveAll()
    {
        _status.Text = string.Empty;
        var root = _repoRoot.Text.Trim();
        var host = _host.Text.Trim();
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
        {
            MessageBox.Show(this, "Choose a valid repository root folder.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrEmpty(host))
        {
            MessageBox.Show(this, "Enter this PC's current IP or PC name (e.g. 10.0.157.138 or 192.168.0.4).", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var apiDir = Path.Combine(root, "open-source-pos");
        var feDir = Path.Combine(root, "open-source-pos-frontend", "src", "assets");
        if (!Directory.Exists(apiDir) || !Directory.Exists(feDir))
        {
            MessageBox.Show(this, "Expected folders not found:\r\n" + apiDir + "\r\n" + feDir, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var settings = new NetworkConfigWriter.LanSettings(
            host,
            (int)_angularPort.Value,
            (int)_httpApiPort.Value,
            (int)_httpsApiPort.Value,
            (int)_imagePort.Value);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LastRepoPathFile)!);
            File.WriteAllText(LastRepoPathFile, root);

            NetworkConfigWriter.SaveAll(root, settings);

            _status.Text = $"Saved for {host}. Restart API + ng serve.";
            MessageBox.Show(this,
                "Saved all network config files:\r\n\r\n" +
                "• open-source-pos/appsettings.Local.json\r\n" +
                "• open-source-pos/appsettings.Development.json (CORS origins)\r\n" +
                "• open-source-pos-frontend/src/assets/app-runtime-config.json\r\n\r\n" +
                "Restart the API and Angular dev server (ng serve) so changes apply.",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
