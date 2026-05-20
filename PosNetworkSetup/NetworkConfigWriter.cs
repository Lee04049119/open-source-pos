using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PosNetworkSetup;

/// <summary>
/// Writes LAN IP to appsettings.Local.json, app-runtime-config.json, and merges CORS in appsettings.Development.json.
/// </summary>
internal static class NetworkConfigWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly string[] LocalhostOriginPrefixes =
    [
        "http://localhost:",
        "https://localhost:",
        "http://127.0.0.1:",
        "https://127.0.0.1:",
    ];

    public sealed record LanSettings(string Host, int AngularPort, int HttpApiPort, int HttpsApiPort, int ImagePort);

    public static string? TryDetectLanIPv4()
    {
        try
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                    continue;
                if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    continue;

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;
                    var ip = ua.Address.ToString();
                    if (IPAddress.IsLoopback(ua.Address))
                        continue;
                    if (ip.StartsWith("169.254.", StringComparison.Ordinal))
                        continue;
                    return ip;
                }
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    public static LanSettings? TryLoadFromRepo(string repoRoot)
    {
        var localPath = Path.Combine(repoRoot, "open-source-pos", "appsettings.Local.json");
        if (!File.Exists(localPath))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(localPath));
            var lan = doc.RootElement.GetProperty("Lan");
            return new LanSettings(
                lan.GetProperty("Host").GetString() ?? "",
                lan.TryGetProperty("AngularPort", out var a) ? a.GetInt32() : 4200,
                lan.TryGetProperty("HttpApiPort", out var h) ? h.GetInt32() : 5000,
                lan.TryGetProperty("HttpsApiPort", out var hs) ? hs.GetInt32() : 5001,
                lan.TryGetProperty("ImagePort", out var i) ? i.GetInt32() : 9096);
        }
        catch
        {
            return null;
        }
    }

    public static string[] BuildCorsOrigins(string host, int angularPort, int httpApiPort, int httpsApiPort, int imagePort) =>
    [
        $"http://{host}:{angularPort}",
        $"https://{host}:{angularPort}",
        $"http://{host}:{httpApiPort}",
        $"https://{host}:{httpsApiPort}",
        $"http://{host}:{imagePort}",
        $"https://{host}:{imagePort}",
    ];

    public static void SaveAll(string repoRoot, LanSettings settings)
    {
        var apiDir = Path.Combine(repoRoot, "open-source-pos");
        var feAssetsDir = Path.Combine(repoRoot, "open-source-pos-frontend", "src", "assets");

        var localJson = new
        {
            Lan = new
            {
                Host = settings.Host,
                AngularPort = settings.AngularPort,
                HttpApiPort = settings.HttpApiPort,
                HttpsApiPort = settings.HttpsApiPort,
                ImagePort = settings.ImagePort,
            },
        };

        var runtime = new
        {
            apiBaseUrl = $"http://{settings.Host}:{settings.HttpApiPort}/api",
            apiBaseUrlHttps = $"https://{settings.Host}:{settings.HttpsApiPort}/api",
            imageServerUrl = $"http://{settings.Host}:{settings.ImagePort}/",
            imageServerUrlHttps = $"https://{settings.Host}:{settings.ImagePort}/",
        };

        File.WriteAllText(
            Path.Combine(apiDir, "appsettings.Local.json"),
            JsonSerializer.Serialize(localJson, JsonOptions));

        File.WriteAllText(
            Path.Combine(feAssetsDir, "app-runtime-config.json"),
            JsonSerializer.Serialize(runtime, JsonOptions));

        UpdateDevelopmentCors(apiDir, settings);
    }

    private static void UpdateDevelopmentCors(string apiDir, LanSettings settings)
    {
        var devPath = Path.Combine(apiDir, "appsettings.Development.json");
        if (!File.Exists(devPath))
            return;

        var root = JsonNode.Parse(File.ReadAllText(devPath)) as JsonObject
            ?? throw new InvalidOperationException("Invalid appsettings.Development.json");

        if (root["Cors"] is not JsonObject cors)
        {
            cors = new JsonObject();
            root["Cors"] = cors;
        }

        var newLanOrigins = BuildCorsOrigins(
            settings.Host,
            settings.AngularPort,
            settings.HttpApiPort,
            settings.HttpsApiPort,
            settings.ImagePort);

        var merged = new List<string>();

        if (cors["AllowedOrigins"] is JsonArray existing)
        {
            foreach (var node in existing)
            {
                var value = node?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                if (IsLocalhostOrigin(value))
                    merged.Add(value);
            }
        }

        foreach (var origin in newLanOrigins)
        {
            if (!merged.Contains(origin, StringComparer.OrdinalIgnoreCase))
                merged.Add(origin);
        }

        cors["AllowedOrigins"] = new JsonArray(merged.Select(o => JsonValue.Create(o)).ToArray());
        File.WriteAllText(devPath, root.ToJsonString(JsonOptions));
    }

    private static bool IsLocalhostOrigin(string origin)
    {
        foreach (var prefix in LocalhostOriginPrefixes)
        {
            if (origin.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
