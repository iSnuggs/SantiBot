"use client";

import { useState } from "react";
import ConfigPanel, { Toggle, InputField, SelectField } from "@/components/ConfigPanel";

interface PurgeConfig {
  id: string;
  channelName: string;
  interval: string;
  keepPinned: boolean;
  active: boolean;
}

export default function AutoPurgePage() {
  const [enabled, setEnabled] = useState(true);
  const [logDeletions, setLogDeletions] = useState(false);
  const [logChannel, setLogChannel] = useState("");
  const [hasChanges, setHasChanges] = useState(false);

  const [channels] = useState<PurgeConfig[]>([
    { id: "1", channelName: "#temp-chat", interval: "6h", keepPinned: true, active: true },
    { id: "2", channelName: "#bot-commands", interval: "24h", keepPinned: false, active: true },
    { id: "3", channelName: "#memes", interval: "7d", keepPinned: true, active: false },
  ]);

  const handleChange = (setter: Function) => (value: any) => {
    setter(value);
    setHasChanges(true);
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Auto Purge</h1>
      <p className="text-[var(--muted)] mb-6">
        Automatically delete messages in specified channels on a schedule to keep your server clean.
      </p>

      <div className="space-y-6">
        <ConfigPanel
          title="Auto Purge Settings"
          hasChanges={hasChanges}
          onSave={() => setHasChanges(false)}
          onDiscard={() => setHasChanges(false)}
        >
          <Toggle label="Enable Auto Purge" checked={enabled} onChange={handleChange(setEnabled)} />
          <Toggle label="Log Deletions" checked={logDeletions} onChange={handleChange(setLogDeletions)} />
          <InputField
            label="Log Channel ID"
            value={logChannel}
            onChange={handleChange(setLogChannel)}
            placeholder="Enter channel ID for purge logs"
          />
        </ConfigPanel>

        <ConfigPanel title="Channel Configurations">
          {channels.length === 0 ? (
            <p className="text-[var(--muted)] text-sm">No auto-purge channels configured.</p>
          ) : (
            <div className="space-y-3">
              {channels.map((ch) => (
                <div
                  key={ch.id}
                  className="flex items-center justify-between p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]"
                >
                  <div>
                    <p className="text-sm font-medium">{ch.channelName}</p>
                    <p className="text-xs text-[var(--muted)]">
                      Every {ch.interval} &middot; {ch.keepPinned ? "Keeps pinned" : "Purges all"}
                    </p>
                  </div>
                  <span
                    className={`text-xs px-2 py-1 rounded-full ${
                      ch.active
                        ? "bg-green-500/20 text-green-400"
                        : "bg-[var(--border)] text-[var(--muted)]"
                    }`}
                  >
                    {ch.active ? "Active" : "Paused"}
                  </span>
                </div>
              ))}
            </div>
          )}
        </ConfigPanel>
      </div>
    </div>
  );
}
