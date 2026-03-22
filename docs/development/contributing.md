# Contributing to SantiBot

Thank you for your interest in contributing!

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/YOUR_USERNAME/SantiBot.git`
3. Create a feature branch: `git checkout -b feature/my-feature`
4. Make your changes
5. Build and test: `dotnet build && dotnet test`
6. Commit and push
7. Open a Pull Request against the `develop` branch

## Development Setup

### Requirements

- .NET 8 SDK
- Node.js 18+ (for dashboard)
- Git

### Building

```bash
dotnet build SantiBot.sln
```

### Running Tests

```bash
dotnet test SantiBot.sln
```

### Running the Bot (Development)

```bash
dotnet run --project src/SantiBot
```

### Running the Dashboard (Development)

```bash
# API
dotnet run --project src/SantiBot.Dashboard

# Frontend
cd dashboard && npm install && npm run dev
```

## Project Structure

```
SantiBot.sln
├── src/SantiBot/                   Main bot application
│   ├── Modules/                    Feature modules (commands + services)
│   ├── Db/                         Database context and models
│   └── _common/                    Shared utilities and base classes
├── src/SantiBot.Dashboard/         Web API backend
├── src/Santi.Medusa/               Plugin SDK
├── src/SantiBot.Voice/             Voice/audio processing
├── src/SantiBot.Coordinator/       Multi-shard coordination
├── src/SantiBot.Tests/             Unit tests
├── dashboard/                      Next.js frontend
├── installer/                      Tauri installer
└── docs/                           Documentation
```

## Adding a New Feature

1. Create a new directory under `src/SantiBot/Modules/{Category}/YourFeature/`
2. Add `YourFeatureCommands.cs` (extends `SantiModule<YourFeatureService>`)
3. Add `YourFeatureService.cs` (implements `INService`)
4. Add `strings/res.yml` (localization), `strings/names.yml` (aliases), `strings/cmds.yml` (help text)
5. Add database models in `src/SantiBot/Db/Models/` if needed
6. Register new DbSets in `SantiContext.cs`
7. Add locale stub files for all languages
8. Write tests in `src/SantiBot.Tests/`

## Code Style

- Follow existing patterns in the codebase
- Use the `SantiModule<TService>` base class for command modules
- Use `INService` for services that should be auto-registered
- Use `IReadyExecutor` for services that need to run on bot startup
- Localize all user-facing strings

## License

SantiBot is licensed under AGPLv3. All contributions must be compatible with this license.
