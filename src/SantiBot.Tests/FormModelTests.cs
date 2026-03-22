using System;
using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;
using SantiBot.Db.Models;

namespace SantiBot.Tests;

public class FormModelTests
{
    [Test]
    public void FormModel_IsActive_DefaultsToTrue()
    {
        var form = new FormModel();
        Assert.That(form.IsActive, Is.True);
    }

    [Test]
    public void FormModel_QuestionsJson_DeserializesCorrectly()
    {
        var questions = new List<string> { "What is your name?", "Why do you want to join?" };
        var json = JsonSerializer.Serialize(questions);
        var form = new FormModel { QuestionsJson = json };

        var deserialized = JsonSerializer.Deserialize<List<string>>(form.QuestionsJson);
        Assert.That(deserialized, Has.Count.EqualTo(2));
    }

    [Test]
    public void FormResponse_AnswersJson_SerializesCorrectly()
    {
        var answers = new List<string> { "Snuggs", "Because it's awesome!" };
        var json = JsonSerializer.Serialize(answers);
        var response = new FormResponse { AnswersJson = json, FormId = 1, UserId = 123UL };

        Assert.That(response.FormId, Is.EqualTo(1));
        var deserialized = JsonSerializer.Deserialize<List<string>>(response.AnswersJson);
        Assert.That(deserialized![1], Is.EqualTo("Because it's awesome!"));
    }

    [Test]
    public void AutoPurgeConfig_IsActive_DefaultsToTrue()
    {
        var config = new AutoPurgeConfig();
        Assert.That(config.IsActive, Is.True);
    }

    [Test]
    public void AutoPurgeConfig_Properties_SetCorrectly()
    {
        var config = new AutoPurgeConfig
        {
            GuildId = 1UL,
            ChannelId = 2UL,
            IntervalHours = 24,
            MaxMessageAgeHours = 48,
        };

        Assert.That(config.IntervalHours, Is.EqualTo(24));
        Assert.That(config.MaxMessageAgeHours, Is.EqualTo(48));
    }
}
