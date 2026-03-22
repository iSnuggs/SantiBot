using System;
using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;
using SantiBot.Db.Models;

namespace SantiBot.Tests;

public class PollModelTests
{
    [Test]
    public void PollModel_IsActive_DefaultsToTrue()
    {
        var poll = new PollModel();
        Assert.That(poll.IsActive, Is.True);
    }

    [Test]
    public void PollModel_EndsAt_CanBeNull()
    {
        var poll = new PollModel { EndsAt = null };
        Assert.That(poll.EndsAt, Is.Null);
    }

    [Test]
    public void PollModel_OptionsJson_SerializesCorrectly()
    {
        var options = new List<string> { "Option A", "Option B", "Option C" };
        var json = JsonSerializer.Serialize(options);
        var poll = new PollModel { OptionsJson = json };

        var deserialized = JsonSerializer.Deserialize<List<string>>(poll.OptionsJson);
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Count, Is.EqualTo(3));
        Assert.That(deserialized[0], Is.EqualTo("Option A"));
    }

    [Test]
    public void SuggestionModel_Status_DefaultsToPending()
    {
        var suggestion = new SuggestionModel();
        Assert.That(suggestion.Status, Is.EqualTo(SuggestionStatus.Pending));
    }

    [Test]
    public void SuggestionStatus_HasAllValues()
    {
        var values = Enum.GetValues<SuggestionStatus>();
        Assert.That(values, Has.Length.EqualTo(4));
        Assert.That(values, Contains.Item(SuggestionStatus.Pending));
        Assert.That(values, Contains.Item(SuggestionStatus.Approved));
        Assert.That(values, Contains.Item(SuggestionStatus.Denied));
        Assert.That(values, Contains.Item(SuggestionStatus.Implemented));
    }
}
