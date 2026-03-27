"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel, { InputField } from "@/components/ConfigPanel";

const logEvents = [
  { key: "messageUpdated", label: "Message Updated", icon: "✏️" },
  { key: "messageDeleted", label: "Message Deleted", icon: "🗑️" },
  { key: "userJoined", label: "User Joined", icon: "📥" },
  { key: "userLeft", label: "User Left", icon: "📤" },
  { key: "userBanned", label: "User Banned", icon: "🔨" },
  { key: "userUnbanned", label: "User Unbanned", icon: "✅" },
  { key: "userUpdated", label: "User Updated", icon: "👤" },
  { key: "nicknameChanged", label: "Nickname Changed", icon: "👥" },
  { key: "roleChanged", label: "Role Changed", icon: "⚔️" },
  { key: "channelCreated", label: "Channel Created", icon: "➕" },
  { key: "channelDestroyed", label: "Channel Destroyed", icon: "➖" },
  { key: "channelUpdated", label: "Channel Updated", icon: "📝" },
  { key: "threadCreated", label: "Thread Created", icon: "🧵" },
  { key: "threadDeleted", label: "Thread Deleted", icon: "🗑️" },
  { key: "voicePresence", label: "Voice Activity", icon: "🔊" },
  { key: "userMuted", label: "User Muted", icon: "🔇" },
  { key: "userWarned", label: "User Warned", icon: "⚠️" },
  { key: "emojiUpdated", label: "Emoji Updated", icon: "😀" },
];

export default function LoggingPage() {
  const { guildId } = useParams();
  const [channels, setChannels] = useState<Record<string, string>>({});
  const [originalChannels, setOriginalChannels] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);

  useEffect(() => {
    if (!guildId) return;

    setLoading(true);
    apiFetch<Record<string, string>>(`/api/guilds/${guildId}/config/logging`)
      .then((res) => {
        const channelData: Record<string, string> = {};
        for (const event of logEvents) {
          channelData[event.key] = res[event.key] || "";
        }
        setChannels(channelData);
        setOriginalChannels({ ...channelData });
        setError(null);
      })
      .catch((err) => {
        setError(err.message || "Failed to load logging config");
      })
      .finally(() => setLoading(false));
  }, [guildId]);

  const setChannel = (key: string, value: string) => {
    setChannels((prev) => ({ ...prev, [key]: value }));
    setHasChanges(true);
  };

  const handleDiscard = () => {
    setChannels({ ...originalChannels });
    setHasChanges(false);
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      await apiFetch(`/api/guilds/${guildId}/config/logging`, {
        method: "PATCH",
        body: JSON.stringify(channels),
      });
      setOriginalChannels({ ...channels });
      setHasChanges(false);
      setError(null);
    } catch (err: any) {
      setError(err.message || "Failed to save logging config");
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="animate-spin rounded-full h-8 w-8 border-2 border-[var(--accent)] border-t-transparent" />
      </div>
    );
  }

  if (error && Object.keys(originalChannels).length === 0) {
    return (
      <div className="bg-red-500/10 border border-red-500/30 rounded-xl p-6 text-center">
        <p className="text-red-400 font-medium">Failed to load logging settings</p>
        <p className="text-red-400/70 text-sm mt-1">{error}</p>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Server Logging</h1>
      <p className="text-[var(--muted)] mb-6">
        Configure which events are logged and where. Each event type can be sent
        to a different channel.
      </p>

      {error && (
        <div className="bg-red-500/10 border border-red-500/30 rounded-xl p-4 mb-6">
          <p className="text-red-400 text-sm">{error}</p>
        </div>
      )}

      <ConfigPanel
        title="Log Event Channels"
        description="Set a channel ID for each event type you want to log. Leave empty to disable."
        hasChanges={hasChanges}
        onSave={handleSave}
        onDiscard={handleDiscard}
      >
        <div className="grid grid-cols-1 md:grid-cols-2 gap-x-6 gap-y-1">
          {logEvents.map((event) => (
            <InputField
              key={event.key}
              label={`${event.icon} ${event.label}`}
              value={channels[event.key] || ""}
              onChange={(v) => setChannel(event.key, v)}
              placeholder="Channel ID"
            />
          ))}
        </div>
      </ConfigPanel>
    </div>
  );
}
