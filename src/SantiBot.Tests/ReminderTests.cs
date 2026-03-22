using System;
using NUnit.Framework;
using SantiBot.Db.Models;

namespace SantiBot.Tests;

public class ReminderTests
{
    [Test]
    public void Reminder_RecurrenceInterval_DefaultsToNull()
    {
        var reminder = new Reminder();
        Assert.That(reminder.RecurrenceInterval, Is.Null);
    }

    [Test]
    public void Reminder_RecurrenceInterval_CanBeSet()
    {
        var interval = TimeSpan.FromHours(2);
        var reminder = new Reminder { RecurrenceInterval = interval };
        Assert.That(reminder.RecurrenceInterval, Is.EqualTo(interval));
    }

    [Test]
    public void Reminder_RecurrenceInterval_PreservesMinutes()
    {
        var interval = TimeSpan.FromMinutes(30);
        var reminder = new Reminder { RecurrenceInterval = interval };
        Assert.That(reminder.RecurrenceInterval!.Value.TotalMinutes, Is.EqualTo(30));
    }
}
