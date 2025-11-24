using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var store = new ConcurrentDictionary<string, (double idle, DateTime lastUpdate)>();

var apiKey = Environment.GetEnvironmentVariable("MONITOR_API_KEY") ?? "changeme";

app.MapPost("/update", async (HttpRequest req) =>
{
    if (!req.Headers.TryGetValue("Authorization", out var auth) ||
        auth.ToString() != $"Bearer {apiKey}")
        return Results.Unauthorized();

    var data = await req.ReadFromJsonAsync<UpdateRequest>();
    if (data == null) return Results.BadRequest();

    store["main-pc"] = (data.Idle, DateTime.UtcNow);
    return Results.Ok(new { ok = true });
});

app.MapGet("/status", () =>
{
    if (store.TryGetValue("main-pc", out var v))
    {
        var status = v.idle > 30 ? "IDLE" : "ACTIVE";
        return Results.Json(new { idle = v.idle, status, lastUpdate = v.lastUpdate });
    }
    return Results.Json(new { idle = 0.0, status = "UNKNOWN", lastUpdate = (string?)null });
});

app.MapGet("/", async ctx =>
{
    double idle = 0;
    string status = "UNKNOWN";
    DateTime? lu = null;

    if (store.TryGetValue("main-pc", out var v))
    {
        idle = v.idle;
        status = idle > 30 ? "IDLE" : "ACTIVE";
        lu = v.lastUpdate;
    }

    string bgColor = status switch
    {
        "ACTIVE" => "#00c853",
        "IDLE" => "#ffab00",
        _ => "#9e9e9e"
    };

    var html = $@"
<html>
<head>
<style>
body {{
    background:#111;
    color:#fff;
    font-family:Arial;
    text-align:center;
    padding:60px;
}}

.box {{
    font-size:48px;
    padding:20px;
    border-radius:10px;
    background:{bgColor};
    color:black;
    width: 80%;
    margin: auto;
    transition: all 0.25s ease;
    cursor: pointer;
}}

.box:hover {{
    animation: pulse 0.6s infinite alternate ease-in-out;
    filter: brightness(1.2);
}}

@keyframes pulse {{
    from {{ transform: scale(1.00); }}
    to   {{ transform: scale(1.05); }}
}}

.footer {{
    margin-top:20px;
    font-size: 14px;
    color: #aaa;
}}
</style>
</head>

<body>

<div class='box'>{status}</div>

<p>Idle: <span id='idle'>{idle:F1}</span> seconds</p>

<p style='font-size:12px;color:#bbb'>
Last update: {(lu.HasValue ? lu.Value.ToLocalTime().ToString("HH:mm:ss dd/MM/yyyy") : "never")}
</p>

<div class='footer'>PC Idle Monitor • Smooth real-time updates • Vietnam Time (UTC+7)</div>

<script>
// Idle tăng mượt theo thời gian thực
let currentIdle = {{idle}};
let currentStatus = ""{{status}}"";

// Chỉ đếm khi KHÔNG phải UNKNOWN
if (currentStatus !== ""UNKNOWN"") {{
    setInterval(() => {{
        currentIdle += 0.1;
        document.querySelector('#idle').innerText = currentIdle.toFixed(1);
    }}, 100);
}}

</script>

</body>
</html>";

    ctx.Response.ContentType = "text/html; charset=utf-8";
    await ctx.Response.WriteAsync(html);
});

app.Run();

// Record fix
record UpdateRequest(double Idle);
