using NUnit.Framework;
using SantiBot.Db.Models;

namespace SantiBot.Tests;

public class GiveawayTests
{
    [Test]
    public void GiveawayModel_DefaultWinnerCount_IsOne()
    {
        var ga = new GiveawayModel();
        Assert.That(ga.WinnerCount, Is.EqualTo(1));
    }

    [Test]
    public void GiveawayModel_RequiredRoleId_DefaultsToNull()
    {
        var ga = new GiveawayModel();
        Assert.That(ga.RequiredRoleId, Is.Null);
    }

    [Test]
    public void GiveawayModel_WinnerCount_CanBeSet()
    {
        var ga = new GiveawayModel { WinnerCount = 5 };
        Assert.That(ga.WinnerCount, Is.EqualTo(5));
    }

    [Test]
    public void GiveawayModel_RequiredRole_CanBeSet()
    {
        var ga = new GiveawayModel { RequiredRoleId = 123456789UL };
        Assert.That(ga.RequiredRoleId, Is.EqualTo(123456789UL));
    }

    [Test]
    public void GiveawayModel_Participants_DefaultsToEmptyList()
    {
        var ga = new GiveawayModel();
        Assert.That(ga.Participants, Is.Not.Null);
        Assert.That(ga.Participants.Count, Is.EqualTo(0));
    }
}
