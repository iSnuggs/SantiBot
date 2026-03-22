# Dashboard Setup

SantiBot includes a web dashboard for managing your server settings from a browser.

## Architecture

- **Backend:** ASP.NET Core Web API (`src/SantiBot.Dashboard/`)
- **Frontend:** Next.js 14 React app (`dashboard/`)
- **Authentication:** Discord OAuth2 with JWT sessions

## Prerequisites

- Discord Application with OAuth2 configured
- Node.js 18+ (for the frontend)
- The SantiBot backend running

## Setup

### 1. Configure Discord OAuth2

Go to [Discord Developer Portal](https://discord.com/developers/applications) > Your App > OAuth2:

- Add a redirect URL: `http://localhost:5000/api/auth/callback`
- Note your **Client ID** and **Client Secret**

### 2. Configure the Dashboard API

Edit `src/SantiBot.Dashboard/appsettings.json`:

```json
{
  "Discord": {
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "RedirectUri": "http://localhost:5000/api/auth/callback"
  },
  "Jwt": {
    "Key": "CHANGE_THIS_TO_A_RANDOM_32_CHAR_STRING"
  },
  "Dashboard": {
    "FrontendUrl": "http://localhost:3000"
  }
}
```

### 3. Run the API

```bash
dotnet run --project src/SantiBot.Dashboard
```

### 4. Run the Frontend

```bash
cd dashboard
npm install
npm run dev
```

### 5. Access the Dashboard

Open `http://localhost:3000` and click "Login with Discord".

## Production Deployment

For production, use a reverse proxy (nginx/caddy) with HTTPS:

```
https://dashboard.yourdomain.com → Frontend (port 3000)
https://api.yourdomain.com → API (port 5000)
```

Update the OAuth2 redirect URI and `appsettings.json` accordingly.
