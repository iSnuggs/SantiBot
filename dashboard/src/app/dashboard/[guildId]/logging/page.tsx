"use client";

import { useState } from "react";
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
  const [channels, setChannels] = useState<Record<string, string>>({});
  const [hasChanges, setHasChanges] = useState(false);

  const setChannel = (key: string, value: string) => {
    setChannels((prev) => ({ ...prev, [key]: value }));
    setHasChanges(true);
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Server Logging</h1>
      <p className="text-[var(--muted)] mb-6">
        Configure which events are logged and where. Each event type can be sent to a different channel.
      </p>

      <ConfigPanel
        title="Log Event Channels"
        description="Set a channel ID for each event type you want to log. Leave empty to disable."
        hasChanges={hasChanges}
        onSave={() => setHasChanges(false)}
        onDiscard={() => setHasChanges(false)}
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
