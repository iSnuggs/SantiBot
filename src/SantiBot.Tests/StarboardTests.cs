using NUnit.Framework;
using SantiBot.Db.Models;

namespace SantiBot.Tests;

public class StarboardTests
{
    [Test]
    public void StarboardSettings_DefaultThreshold_IsThree()
    {
        var settings = new StarboardSettings();
        Assert.That(settings.StarThreshold, Is.EqualTo(3));
    }

    [Test]
    public void StarboardSettings_DefaultEmoji_IsStar()
    {
        var settings = new StarboardSettings();
        Assert.That(settings.StarEmoji, Is.EqualTo("⭐"));
    }

    [Test]
    public void StarboardSettings_AllowSelfStar_DefaultsFalse()
    {
        var settings = new StarboardSettings();
        Assert.That(settings.AllowSelfStar, Is.False);
    }

    [Test]
    public void StarboardEntry_Properties_SetCorrectly()
    {
        var entry = new StarboardEntry
        {
            GuildId = 1UL,
            ChannelId = 2UL,
            MessageId = 3UL,
            AuthorId = 4UL,
            StarboardMessageId = 5UL,
            StarCount = 10,
        };

        Assert.That(entry.GuildId, Is.EqualTo(1UL));
        Assert.That(entry.StarCount, Is.EqualTo(10));
        Assert.That(entry.StarboardMessageId, Is.EqualTo(5UL));
    }
}
