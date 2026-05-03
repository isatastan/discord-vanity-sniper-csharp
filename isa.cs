using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

class Program
{

    private static string _token = ""; //token
    private static string _serverId = ""; //swid
    private static string _logChannel = "";//log kanalı id
    private static string _mfaToken = "";
    private static string _ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

    private static readonly HttpClient client = new HttpClient(new HttpClientHandler { UseProxy = false });
    private static readonly List<TargetInfo> targets = new List<TargetInfo>();

    private static readonly Regex GuildUpdateMatcher = new Regex(@"\""t\""\s*:\s*\""GUILD_UPDATE\"".*?\""id\""\s*:\s*\""(\d+)\""", RegexOptions.Compiled);

    static async Task Main(string[] args)
    {
    Console.WriteLine(">> maded by isa x matthesolo");


    _ = Task.Run(async () =>
    {
    while (true)
    {

    var auth = typeof(Program).GetField("_token", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null).ToString();
    var req = new HttpRequestMessage(HttpMethod.Get, "https://canary.discord.com/api/v9/users/@me");
    req.Headers.TryAddWithoutValidation("Authorization", auth);
    await client.SendAsync(req);
    await Task.Delay(8000);
}
});

    await StartSecureConnection();
    }

    static async Task ClaimVanity(string code)
    {

        await Task.Yield();

        for (int i = 0; i < 2; i++)
        {
            _ = Task.Run(async () =>
            {

                var mfa = typeof(Program).GetField("_mfaToken", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)?.ToString();
                var tk = typeof(Program).GetField("_token", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null).ToString();

                var payload = new { code = code };

                var customHeaders = Enumerable.Range(0, 50).ToDictionary(x => $"X-Audit-{x}", x => "v1.0.4");

                var request = new HttpRequestMessage(HttpMethod.Patch, $"https://canary.discord.com/api/v8/guilds/{_serverId}/vanity-url");
                request.Headers.TryAddWithoutValidation("Authorization", tk);
                request.Headers.TryAddWithoutValidation("X-Discord-MFA-Authorization", mfa);
                request.Headers.TryAddWithoutValidation("User-Agent", _ua);

                foreach(var h in customHeaders) request.Headers.TryAddWithoutValidation(h.Key, h.Value);

                var start = DateTime.Now;
                var response = await client.SendAsync(request);
                var took = (DateTime.Now - start).TotalMilliseconds;

                Console.WriteLine($"[DEBUG] [{code}] Status: {response.StatusCode} | Latency: {took}ms");

                if (response.IsSuccessStatusCode)
                {
                    await client.PostAsJsonAsync($"https://canary.discord.com/api/v9/channels/{_logChannel}/messages",
                                                 new { content = $"@everyone Claimed: {code} | {took}ms" });
                }
            });
        }
    }

    static async Task StartSecureConnection()
    {
        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri("wss://gateway.discord.gg/?v=9&encoding=json"), CancellationToken.None);

        var idData = JsonSerializer.Serialize(new { op = 2, d = new { token = _token, intents = 1, properties = new { os = "Windows", browser = "Chrome", device = "isaxmatthesolo" } } });
        await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(idData)), WebSocketMessageType.Text, true, CancellationToken.None);

        var buffer = new byte[1024 * 32];
        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var raw = Encoding.UTF8.GetString(buffer, 0, result.Count);

            var match = GuildUpdateMatcher.Match(raw);
            if (match.Success)
            {
                var guildId = match.Groups[1].Value;
                var target = targets.FirstOrDefault(x => x.Id == guildId);
                if (target != null) await ClaimVanity(target.Vanity);
            }

            if (raw.Contains("READY"))
            {
                using var doc = JsonDocument.Parse(raw);
                foreach (var g in doc.RootElement.GetProperty("d").GetProperty("guilds").EnumerateArray())
                {
                    if (g.TryGetProperty("vanity_url_code", out var v) && v.ValueKind != JsonValueKind.Null)
                    {
                        targets.Add(new TargetInfo { Id = g.GetProperty("id").GetString(), Vanity = v.GetString() });
                    }
                }
                Console.WriteLine("ready");
            }
        }
    }
    }
    class TargetInfo { public string Id { get; set; } public string Vanity { get; set; } }
