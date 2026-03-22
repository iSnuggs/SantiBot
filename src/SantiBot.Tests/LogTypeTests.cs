using System;
using System.Collections.Generic;
using NUnit.Framework;
using SantiBot.Common;

namespace SantiBot.Tests;

public class LogTypeTests
{
    [Test]
    public void LogType_HasAllExpectedValues()
    {
        var values = Enum.GetNames<LogType>();

        Assert.That(values, Contains.Item("NicknameChanged"));
        Assert.That(values, Contains.Item("RoleChanged"));
        Assert.That(values, Contains.Item("EmojiUpdated"));
    }

    [Test]
    public void LogType_HasOriginalValues()
    {
        var values = Enum.GetNames<LogType>();

        Assert.That(values, Contains.Item("MessageUpdated"));
        Assert.That(values, Contains.Item("MessageDeleted"));
        Assert.That(values, Contains.Item("UserJoined"));
        Assert.That(values, Contains.Item("UserLeft"));
        Assert.That(values, Contains.Item("UserBanned"));
        Assert.That(values, Contains.Item("VoicePresence"));
        Assert.That(values, Contains.Item("ThreadCreated"));
        Assert.That(values, Contains.Item("ThreadDeleted"));
    }

    [Test]
    public void LogType_TotalCount_Is20()
    {
        // 17 original + 3 new (NicknameChanged, RoleChanged, EmojiUpdated)
        var values = Enum.GetValues<LogType>();
        Assert.That(values, Has.Length.EqualTo(20));
    }
}
