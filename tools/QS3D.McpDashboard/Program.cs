using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

const int DashboardPort = 3220;
const string DashboardHeader = "X-QS3D-Dashboard";
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls($"http://127.0.0.1:{DashboardPort}");
var app = builder.Build();
var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
{
    Timeout = TimeSpan.FromSeconds(3)
};
http.DefaultRequestHeaders.UserAgent.ParseAdd("QS3D-McpDashboard/1.0");

app.Use(async (ctx, next) =>
{
    var host = ctx.Request.Host.Host;
    if (!string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Response.StatusCode = 400;
        return;
    }
    ctx.Response.Headers.CacheControl = "no-store";
    ctx.Response.Headers.XFrameOptions = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers.ContentSecurityPolicy = "default-src 'self'; style-src 'unsafe-inline'; script-src 'unsafe-inline'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'";
    await next();
});

app.MapGet("/", () => Results.Content(DashboardUi.Html, "text/html; charset=utf-8"));

app.MapGet("/api/status", async () =>
{
    var local = await McpDiscovery.FindAsync(http);
    JsonElement? control = null;
    string controlError = string.Empty;
    long controlLatency = -1;
    if (local.Port.HasValue)
    {
        var result = await McpDiscovery.GetControlStatusAsync(http, local.Port.Value);
        control = result.Json;
        controlError = result.Error;
        controlLatency = result.LatencyMs;
    }

    var chatgpt = await ProbeAsync(http, "https://chatgpt.com/");
    ProbeResult? openAiHealth = null;
    ProbeResult? cloudflare = null;
    if (control.HasValue)
    {
        if (TryString(control.Value, "openai", "healthUrl", out var healthUrl) && Uri.TryCreate(healthUrl, UriKind.Absolute, out _))
            openAiHealth = await ProbeAsync(http, healthUrl.TrimEnd('/') + "/readyz");
        if (TryString(control.Value, "cloudflare", "publicMcpUrl", out var publicUrl) && Uri.TryCreate(publicUrl, UriKind.Absolute, out _))
            cloudflare = await ProbeAsync(http, publicUrl);
    }

    return Results.Json(new
    {
        utc = DateTimeOffset.UtcNow,
        localMcp = new { up = local.Port.HasValue, port = local.Port, latencyMs = local.LatencyMs, error = local.Error },
        control = control,
        controlLatencyMs = controlLatency,
        controlError,
        chatgptWeb = chatgpt,
        openAiHealth,
        cloudflarePublicReachability = cloudflare
    });
});

app.MapPost("/api/action/{lane}/{action}", async (HttpContext ctx, string lane, string action) =>
{
    if (!IsSameOriginAction(ctx.Request)) return Results.StatusCode(403);
    if (!Allowed(lane, action)) return Results.NotFound();
    if (!ctx.Request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) ?? true)
        return Results.StatusCode(415);

    using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, false, 4096, leaveOpen: false);
    var body = await reader.ReadToEndAsync();
    if (body.Length > 16 * 1024) return Results.StatusCode(413);

    var local = await McpDiscovery.FindAsync(http);
    if (!local.Port.HasValue)
        return Results.Json(new { ok = false, error = "QS3D embedded MCP is not running." }, statusCode: 503);

    var result = await McpDiscovery.PostActionAsync(http, local.Port.Value, lane, action, body);
    return Results.Content(result.Body, "application/json; charset=utf-8", Encoding.UTF8, result.StatusCode);
});

await app.RunAsync();

static bool Allowed(string lane, string action)
{
    var l = lane.ToLowerInvariant();
    var a = action.ToLowerInvariant();
    return (l == "openai" || l == "cloudflare")
           && (a == "start" || a == "stop" || a == "restart" || a == "autostart");
}

static bool IsSameOriginAction(HttpRequest request)
{
    if (!string.Equals(request.Headers[DashboardHeader], "1", StringComparison.Ordinal)) return false;
    var origin = request.Headers.Origin.ToString();
    return string.Equals(origin, $"http://127.0.0.1:{DashboardPort}", StringComparison.OrdinalIgnoreCase)
           || string.Equals(origin, $"http://localhost:{DashboardPort}", StringComparison.OrdinalIgnoreCase);
}

static async Task<ProbeResult> ProbeAsync(HttpClient client, string url)
{
    var sw = Stopwatch.StartNew();
    try
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        sw.Stop();
        return new ProbeResult(true, (int)response.StatusCode, sw.ElapsedMilliseconds, string.Empty);
    }
    catch (Exception ex)
    {
        sw.Stop();
        return new ProbeResult(false, 0, sw.ElapsedMilliseconds, DashboardSafe.Text(ex.Message));
    }
}

static bool TryString(JsonElement root, string group, string name, out string value)
{
    value = string.Empty;
    if (!root.TryGetProperty(group, out var section) || !section.TryGetProperty(name, out var item)) return false;
    value = item.ValueKind == JsonValueKind.String ? item.GetString() ?? string.Empty : string.Empty;
    return value.Length > 0;
}

sealed record ProbeResult(bool Up, int StatusCode, long LatencyMs, string Error);


static class DashboardSafe
{
    internal static string Text(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value[..Math.Min(value.Length, 240)];
}

static class McpDiscovery
{
    private static string TokenPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QS3D", "mcp-bearer-token.txt");

    internal static async Task<(int? Port, long LatencyMs, string Error)> FindAsync(HttpClient client)
    {
        var probes = Enumerable.Range(8765, 16).Select(port => ProbePortAsync(client, port)).ToArray();
        var results = await Task.WhenAll(probes);
        var match = results.Where(x => x.Up).OrderBy(x => x.Port).FirstOrDefault();
        return match.Up
            ? (match.Port, match.LatencyMs, string.Empty)
            : (null, -1, "No QS3D MCP listener found on 127.0.0.1:8765-8780.");
    }

    private static async Task<(bool Up, int Port, long LatencyMs)> ProbePortAsync(HttpClient client, int port)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
            using var response = await client.GetAsync($"http://127.0.0.1:{port}/healthz", cts.Token);
            var body = await response.Content.ReadAsStringAsync(cts.Token);
            sw.Stop();
            return (response.IsSuccessStatusCode && body.Contains("qs3d-bricscad-mcp", StringComparison.Ordinal), port, sw.ElapsedMilliseconds);
        }
        catch
        {
            sw.Stop();
            return (false, port, sw.ElapsedMilliseconds);
        }
    }

    internal static async Task<(JsonElement? Json, long LatencyMs, string Error)> GetControlStatusAsync(HttpClient client, int port)
    {
        var token = ReadToken();
        if (token.Length < 32) return (null, -1, "Local MCP bearer token is unavailable.");
        var sw = Stopwatch.StartNew();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{port}/qs3d/dashboard/status");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            sw.Stop();
            if (!response.IsSuccessStatusCode) return (null, sw.ElapsedMilliseconds, $"Control bridge HTTP {(int)response.StatusCode}.");
            using var doc = JsonDocument.Parse(body);
            return (doc.RootElement.Clone(), sw.ElapsedMilliseconds, string.Empty);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (null, sw.ElapsedMilliseconds, DashboardSafe.Text(ex.Message));
        }
        finally { token = string.Empty; }
    }

    internal static async Task<(int StatusCode, string Body)> PostActionAsync(
        HttpClient client, int port, string lane, string action, string body)
    {
        var token = ReadToken();
        if (token.Length < 32) return (503, "{\"ok\":false,\"error\":\"Local MCP bearer token is unavailable.\"}");
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"http://127.0.0.1:{port}/qs3d/dashboard/{Uri.EscapeDataString(lane)}/{Uri.EscapeDataString(action)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(string.IsNullOrWhiteSpace(body) ? "{}" : body, Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request);
            return ((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex)
        {
            return (503, JsonSerializer.Serialize(new { ok = false, error = DashboardSafe.Text(ex.Message) }));
        }
        finally { token = string.Empty; }
    }

    private static string ReadToken()
    {
        try { return File.Exists(TokenPath) ? File.ReadAllText(TokenPath, Encoding.UTF8).Trim() : string.Empty; }
        catch { return string.Empty; }
    }
}

static class DashboardUi
{
internal const string Html = """
<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>QS3D MCP Local Dashboard</title><style>
:root{font-family:Inter,Segoe UI,sans-serif;color:#e6edf3;background:#0d1117}*{box-sizing:border-box}body{margin:0;padding:24px;max-width:1320px;margin:auto}.top{display:flex;justify-content:space-between;gap:20px;align-items:center}.muted{color:#8b949e}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:14px;margin-top:18px}.card{background:#161b22;border:1px solid #30363d;border-radius:12px;padding:16px}.ok{color:#3fb950}.bad{color:#f85149}.warn{color:#d29922}.metric{font-size:30px;font-weight:700}.topology{display:flex;gap:10px;align-items:center;flex-wrap:wrap;margin:18px 0}.node{padding:10px 14px;border:1px solid #30363d;border-radius:10px;background:#161b22}.arrow{color:#58a6ff;font-size:22px}.row{display:flex;gap:8px;flex-wrap:wrap;margin-top:10px}button{background:#238636;color:white;border:0;border-radius:7px;padding:8px 12px;cursor:pointer}button.secondary{background:#30363d}button.danger{background:#da3633}input{width:100%;background:#0d1117;border:1px solid #30363d;color:#e6edf3;border-radius:7px;padding:9px;margin-top:7px}pre{white-space:pre-wrap;word-break:break-word;font-size:12px;color:#8b949e}.pill{display:inline-block;padding:3px 8px;border-radius:999px;background:#21262d;margin-left:6px}h1,h2,h3{margin:0 0 8px}small{color:#8b949e}</style></head><body>
<div class="top"><div><h1>QS3D MCP Local Dashboard</h1><div class="muted">Loopback-only ? 127.0.0.1:3220 ? OpenAI + Cloudflare independent lanes</div></div><div id="clock" class="muted"></div></div>
<div class="topology"><div class="node">BricsCAD / QS3D MCP</div><div class="arrow">?</div><div class="node">OpenAI Secure Tunnel</div><div class="arrow">?</div><div class="node">ChatGPT</div><div class="node">Cloudflare Named Tunnel</div><div class="arrow">?</div><div class="node">Public HTTPS / OAuth</div></div>
<div class="grid">
<div class="card"><h3>Local MCP</h3><div id="mcpMetric" class="metric">?</div><div id="mcpDetail" class="muted">loading?</div></div>
<div class="card"><h3>ChatGPT web reachability</h3><div id="chatMetric" class="metric">?</div><div id="chatDetail" class="muted">loading?</div></div>
<div class="card"><h3>OpenAI Tunnel</h3><div id="oaState">?</div><div id="oaLatency" class="muted"></div><input id="tunnelId" placeholder="tunnel_... (leave blank to reuse saved)"><input id="runtimeKey" type="password" autocomplete="off" placeholder="Runtime API key (optional; stored by Windows Credential Manager)"><div class="row"><button onclick="act('openai','start',openAiBody())">Start</button><button class="secondary" onclick="act('openai','restart',{})">Restart</button><button class="danger" onclick="act('openai','stop',{})">Stop</button></div><div class="row"><button class="secondary" onclick="auto('openai',true)">Autostart ON</button><button class="secondary" onclick="auto('openai',false)">Autostart OFF</button></div></div>
<div class="card"><h3>Cloudflare Named Tunnel</h3><div id="cfState">?</div><div id="cfLatency" class="muted"></div><div class="row"><button onclick="act('cloudflare','start',{})">Start</button><button class="secondary" onclick="act('cloudflare','restart',{})">Restart</button><button class="danger" onclick="act('cloudflare','stop',{})">Stop</button></div><div class="row"><button class="secondary" onclick="auto('cloudflare',true)">Autostart ON</button><button class="secondary" onclick="auto('cloudflare',false)">Autostart OFF</button></div></div>
</div><div class="card" style="margin-top:14px"><h3>Fault localization</h3><pre id="diag">loading?</pre></div>
<script>
let last={};const q=id=>document.getElementById(id);const cls=x=>x?'ok':'bad';
function ms(v){return Number.isFinite(v)&&v>=0?v+' ms':'?'}
function state(x){if(!x)return 'MCP host unavailable';return `<span class="${cls(x.running)}">${x.running?'RUNNING':'STOPPED'}</span><span class="pill">ready ${x.ready?'yes':'no'}</span><span class="pill">autostart ${x.autostart?'on':'off'}</span>`}
async function refresh(){try{let r=await fetch('/api/status',{cache:'no-store'});last=await r.json();q('clock').textContent=new Date().toLocaleTimeString();q('mcpMetric').className='metric '+cls(last.localMcp.up);q('mcpMetric').textContent=last.localMcp.up?ms(last.localMcp.latencyMs):'DOWN';q('mcpDetail').textContent=last.localMcp.up?'port '+last.localMcp.port:last.localMcp.error;q('chatMetric').className='metric '+cls(last.chatgptWeb.up);q('chatMetric').textContent=last.chatgptWeb.up?ms(last.chatgptWeb.latencyMs):'DOWN';q('chatDetail').textContent=last.chatgptWeb.up?'HTTP '+last.chatgptWeb.statusCode:last.chatgptWeb.error;let c=last.control;q('oaState').innerHTML=state(c?.openai);q('cfState').innerHTML=state(c?.cloudflare);q('oaLatency').textContent='control '+ms(last.controlLatencyMs)+' ? health '+ms(last.openAiHealth?.latencyMs);q('cfLatency').textContent='public reachability '+ms(last.cloudflarePublicReachability?.latencyMs);if(c?.openai?.tunnelId&&!q('tunnelId').value)q('tunnelId').value=c.openai.tunnelId;q('diag').textContent=JSON.stringify({selectedProvider:c?.selectedProvider,controlError:last.controlError,openaiError:c?.openai?.error,cloudflareError:c?.cloudflare?.error,openaiTrust:c?.openai?.trust,cloudflareUrl:c?.cloudflare?.publicMcpUrl},null,2)}catch(e){q('diag').textContent=e.message}}
function openAiBody(){let b={tunnelId:q('tunnelId').value.trim()};let k=q('runtimeKey').value.trim();if(k)b.runtimeApiKey=k;return b}
async function act(lane,action,body){try{let r=await fetch(`/api/action/${lane}/${action}`,{method:'POST',headers:{'Content-Type':'application/json','X-QS3D-Dashboard':'1'},body:JSON.stringify(body)});let j=await r.json();q('runtimeKey').value='';q('diag').textContent=JSON.stringify(j,null,2);await refresh()}catch(e){q('diag').textContent=e.message}}
function auto(lane,enabled){return act(lane,'autostart',{enabled:String(enabled)})}refresh();setInterval(refresh,2500);
</script></body></html>
""";
}
