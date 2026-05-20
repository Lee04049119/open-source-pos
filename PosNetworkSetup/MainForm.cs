using System.Text.Json;
using System.Windows.Forms;

namespace PosNetworkSetup;

/// <summary>
/// Writes appsettings.Local.json (API CORS) and app-runtime-config.json (Angular URLs) from one host + ports.
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
        Text = "POS — LAN / network setup";
        Width = 560;
        Height = 360;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(12),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int r = 0;
        AddRow(panel, r++, "Repository root", RepoRow());
        AddRow(panel, r++, "LAN host / PC name", _host);
        AddRow(panel, r++, "Angular port", _angularPort);
        AddRow(panel, r++, "API HTTP port", _httpApiPort);
        AddRow(panel, r++, "API HTTPS port", _httpsApiPort);
        AddRow(panel, r++, "Image server port", _imagePort);

        var save = new Button { Text = "Save all", Width = 120, Height = 32 };
        save.Click += (_, _) => SaveAll();

        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(12) };
        footer.Controls.Add(save);
        footer.Controls.Add(_status);

        Controls.Add(footer);
        Controls.Add(panel);

        Load += (_, _) => TryLoadLastRepo();

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
            using var dlg = new FolderBrowserDialog { Description = "Select repository root (folder containing open-source-pos and open-source-pos-frontend)" };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                _repoRoot.Text = dlg.SelectedPath;
        };
        var flow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true };
        flow.Controls.Add(_repoRoot);
        flow.Controls.Add(browse);
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
            MessageBox.Show(this, "Enter this PC's current LAN IP or Windows computer name (e.g. 192.168.0.15 or MY-PC).", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var apiDir = Path.Combine(root, "open-source-pos");
        var feDir = Path.Combine(root, "open-source-pos-frontend", "src", "assets");
        if (!Directory.Exists(apiDir) || !Directory.Exists(feDir))
        {
            MessageBox.Show(this, "Expected folders not found:\r\n" + apiDir + "\r\n" + feDir, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var angular = (int)_angularPort.Value;
        var httpApi = (int)_httpApiPort.Value;
        var httpsApi = (int)_httpsApiPort.Value;
        var image = (int)_imagePort.Value;

        var localJson = new
        {
            Lan = new
            {
                Host = host,
                AngularPort = angular,
                HttpApiPort = httpApi,
                HttpsApiPort = httpsApi,
                ImagePort = image,
            },
        };

        var runtime = new
        {
            apiBaseUrl = $"http://{host}:{httpApi}/api",
            apiBaseUrlHttps = $"https://{host}:{httpsApi}/api",
            imageServerUrl = $"http://{host}:{image}/",
            imageServerUrlHttps = $"http://{host}:{image}/",
        };

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LastRepoPathFile)!);
            File.WriteAllText(LastRepoPathFile, root);

            var localPath = Path.Combine(apiDir, "appsettings.Local.json");
            File.WriteAllText(localPath, JsonSerializer.Serialize(localJson, jsonOptions));

            var runtimePath = Path.Combine(feDir, "app-runtime-config.json");
            File.WriteAllText(runtimePath, JsonSerializer.Serialize(runtime, jsonOptions));

            _status.Text = "Saved: appsettings.Local.json + src/assets/app-runtime-config.json. Restart API and ng serve.";
            MessageBox.Show(this, "Saved.\r\n\r\n• API: restart so appsettings.Local.json is read.\r\n• Angular: restart ng serve (or rebuild) so assets pick up app-runtime-config.json.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
