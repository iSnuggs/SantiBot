#nullable disable
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility.Services;

public interface IRemindService
{
    Task AddReminderAsync(ulong userId,
        ulong targetId,
        ulong? guildId,
        bool isPrivate,
        DateTime time,
        string message,
        ReminderType reminderType,
        TimeSpan? recurrenceInterval = null);
}