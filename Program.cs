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

    var html = $@"<html>
<head><meta http-equiv='refresh' content='2'>
<style>
body{{background:#111;color:#fff;font-family:Arial;text-align:center;padding:60px}}
.box{{font-size:48px;padding:20px;border-radius:10px;background:{(status=="ACTIVE"?"green":"orange")};color:black}}
</style>
</head>
<body>
<div class='box'>{status}</div>
<p>Idle: {idle:F1} seconds</p>
<p style='font-size:12px;color:#bbb'>Last update: {(lu.HasValue? lu.Value.ToString("u"):"never")}</p>
</body></html>";
    ctx.Response.ContentType = "text/html; charset=utf-8";
    await ctx.Response.WriteAsync(html);
});

app.Run();

record UpdateRequest(double Idle);
