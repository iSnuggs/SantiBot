namespace SantiBot.Common;

public interface ITimezoneService
{
    TimeZoneInfo GetTimeZoneOrUtc(ulong? guildId);
}