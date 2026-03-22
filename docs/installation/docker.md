# Docker Installation

The easiest way to run SantiBot.

## Requirements

- Docker and Docker Compose installed

## Steps

### 1. Create a directory

```bash
mkdir santibot && cd santibot
```

### 2. Create docker-compose.yml

```yaml
version: "3.8"

services:
  santibot:
    build: https://github.com/iSnuggs/SantiBot.git#develop
    container_name: santibot
    restart: unless-stopped
    volumes:
      - santibot-data:/app/data
    environment:
      - TZ=UTC

volumes:
  santibot-data:
```

### 3. Configure credentials

```bash
docker compose up -d
docker compose exec santibot cat /app/data/creds_example.yml > data/creds.yml
# Edit data/creds.yml with your token and owner ID
docker compose restart
```

### 4. Verify

```bash
docker compose logs -f santibot
```

## Updating

```bash
docker compose pull
docker compose up -d
```

## Useful Commands

```bash
docker compose logs -f          # View logs
docker compose restart           # Restart bot
docker compose down              # Stop bot
docker compose up -d             # Start bot
```
