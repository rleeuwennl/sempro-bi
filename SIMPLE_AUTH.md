# Simple Authorization System

## How It Works

### Server-Side ([WebServiceFun.cs](server/RemoteInvoker/WebServiceFun.cs))
- `/api/auth/login` - POST with password, returns token
- `/api/auth/logout` - POST with X-Auth-Token header
- `IsAuthorized()` - checks if request has valid token in X-Auth-Token header
- Valid tokens stored in memory (cleared on server restart)

### Client-Side ([assets/js/simpleauth.js](assets/js/simpleauth.js))
- Press `Ctrl+Shift+L` to login
- Prompt for password
- Token stored in localStorage
- "Authorized" indicator shown in top-right

## Usage

### For Admin:
1. Press `Ctrl+Shift+L`
2. Enter password: `pgad2026`
3. See green "Authorized" badge
4. Click "Logout" when done

### For Server Requests:
When checking authorization in server code:
```csharp
if (IsAuthorized(request))
{
    // User is authorized - allow edit operation
}
```

The server console shows:
```
[OK] [AUTHORIZED]  ← Green text for authorized requests
[OK]               ← Normal for public requests
```

## Configuration

Change password in [server/RemoteInvoker/WebServiceFun.cs](server/RemoteInvoker/WebServiceFun.cs), line 9:
```csharp
private static readonly string ADMIN_PASSWORD = "pgad2026";
```

## Security Notes

⚠️ **Development only** - This is simple authorization for basic protection, not cryptographically secure.

For production:
- Hash the password with bcrypt or similar
- Use HTTPS (already enforced)
- Consider session expiration
- Use database for token persistence
- Add rate limiting on login endpoint
