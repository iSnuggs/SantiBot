namespace SantiBot.Modules.Owner;

public partial class Owner
{
    [Group("rotatestatus")]
    [Name("StatusRotation")]
    public partial class StatusRotationCommands : SantiModule<StatusRotationService>
    {
        private static readonly string[] _typeNames = ["Playing", "Streaming", "Listening", "Watching", "Competing"];

        [Cmd]
        [OwnerOnly]
        public async Task RotateStatusAdd(int type, [Leftover] string status)
        {
            if (type < 0 || type > 4)
            {
                await Response().Error(strs.rotatestatus_invalid_type).SendAsync();
                return;
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                await Response().Error(strs.rotatestatus_no_text).SendAsync();
                return;
            }

            await _service.AddStatusAsync(status, type);
            await Response().Confirm(strs.rotatestatus_added(_typeNames[type], status)).SendAsync();
        }

        [Cmd]
        [OwnerOnly]
        public Task RotateStatusAdd([Leftover] string status)
            => RotateStatusAdd(0, status);

        [Cmd]
        [OwnerOnly]
        public async Task RotateStatusRemove(int id)
        {
            var removed = await _service.RemoveStatusAsync(id);
            if (removed)
                await Response().Confirm(strs.rotatestatus_removed(id)).SendAsync();
            else
                await Response().Error(strs.rotatestatus_not_found(id)).SendAsync();
        }

        [Cmd]
        [OwnerOnly]
        public async Task RotateStatusList()
        {
            var statuses = _service.ListStatuses();
            if (statuses.Count == 0)
            {
                await Response().Error(strs.rotatestatus_none).SendAsync();
                return;
            }

            var list = string.Join("\n",
                statuses.Select(s =>
                    $"`ID: {s.Id}` | **{(s.Type >= 0 && s.Type < _typeNames.Length ? _typeNames[s.Type] : "Unknown")}** | {s.Status}"));

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Rotating Statuses")
                .WithDescription(list);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [OwnerOnly]
        public async Task RotateStatusClear()
        {
            await _service.ClearAsync();
            await Response().Confirm(strs.rotatestatus_cleared).SendAsync();
        }
    }
}
