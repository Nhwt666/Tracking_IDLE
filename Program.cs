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
        idle = v.idle; status = idle > 30 ? "IDLE" : "ACTIVE"; lu = v.lastUpdate;
    }

    var html = $@"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta http-equiv='refresh' content='2'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>PC Idle Monitor</title>

<style>
    body {{
        background: #0d0d0d;
        margin: 0;
        padding: 40px;
        font-family: 'Segoe UI', Tahoma, sans-serif;
        color: #eee;
        text-align: center;
    }}

    .container {{
        max-width: 700px;
        margin: auto;
    }}

    .status-box {{
        padding: 35px;
        border-radius: 14px;
        font-size: 64px;
        font-weight: 700;
        margin-bottom: 40px;
        color: #fff;
        background: {(status == "ACTIVE" ? "#00c853" : "#ff9100")};
        box-shadow: 0 0 25px {(status == "ACTIVE" ? "#00c853aa" : "#ff9100aa")};
        transition: 0.2s ease-in-out;
    }}

    .info {{
        font-size: 22px;
        margin-top: 10px;
        color: #ccc;
    }}

    .footer {{
        margin-top: 40px;
        font-size: 13px;
        color: #666;
    }}
</style>
</head>

<body>
<div class='container'>
    <div class='status-box'>{status}</div>

    <div class='info'>
        Idle: <b>{idle:F1}</b> seconds
    </div>

    <div class='info' style='font-size:15px;margin-top:15px;'>
        Last update:<br>
        {(lu.HasValue ? lu.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'") : "never")}
    </div>

    <div class='footer'>
        PC Idle Monitor System – Auto refresh every 2s
    </div>
</div>
</body>
</html>";

    ctx.Response.ContentType = "text/html; charset=utf-8";
    await ctx.Response.WriteAsync(html);
});

app.Run();

record UpdateRequest(double Idle);
