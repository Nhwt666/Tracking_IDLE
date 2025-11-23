# IdleMonitorServer

## Deploy on Render

1. Upload this repo to GitHub.
2. On Render → New Web Service → select repo.
3. Build Command:
```
dotnet publish -c Release -o out
```
4. Start Command:
```
dotnet out/IdleMonitorServer.dll
```
5. Add Environment Variable:
```
MONITOR_API_KEY = your-secret-key
```

### API:
POST /update  
GET /status  
GET /
