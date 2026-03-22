// global using System.Collections.Concurrent;
global using NonBlocking;

// packages
global using Serilog;

// nadekobot
global using SantiBot;
global using SantiBot.Db;
global using SantiBot.Services;
global using Santi.Common; // new project
global using SantiBot.Common; // old + nadekobot specific things
global using SantiBot.Common.Attributes;
global using SantiBot.Extensions;

// discord
global using Discord;
global using Discord.Commands;
global using Discord.Net;
global using Discord.WebSocket;

// aliases
global using GuildPerm = Discord.GuildPermission;
global using ChannelPerm = Discord.ChannelPermission;
global using BotPermAttribute = Discord.Commands.RequireBotPermissionAttribute;
global using LeftoverAttribute = Discord.Commands.RemainderAttribute;
global using TypeReaderResult = SantiBot.Common.TypeReaders.TypeReaderResult;

// non-essential
global using JetBrains.Annotations;

// source gen
global using Cloneable;