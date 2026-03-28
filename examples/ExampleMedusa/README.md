# ExampleMedusa — SantiBot Plugin Reference

A minimal example showing how to build a Medusa plugin for SantiBot.

## What's a Medusa?

A Medusa is a plugin that adds new commands and features to SantiBot without modifying the bot's source code. Plugins are loaded and unloaded at runtime.

## Building

```bash
dotnet publish -o bin/medusae/ExampleMedusa /p:DebugType=embedded
```

## Installing

1. Copy the `bin/medusae/ExampleMedusa/` folder into your SantiBot's `data/medusae/` directory
2. In Discord, run: `.meload ExampleMedusa`

## Commands

| Command | Description |
|---------|-------------|
| `.greet` | Bot greets you |
| `.greet @user` | Greet a specific user |
| `.servertime` | Show current server time |

## Unloading

```
.meunload ExampleMedusa
```

## Learn More

See the [Medusa documentation](https://santi.bot/medusa/getting-started/) for the full guide.
