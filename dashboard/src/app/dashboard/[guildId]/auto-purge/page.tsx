"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel, { Toggle, InputField } from "@/components/ConfigPanel";

interface PurgeConfig {
  id: number;
  channelId: number;
  intervalHours: number;
  maxMessageAgeHours: number;
}

export default function AutoPurgePage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Local-only general settings (not wired to API yet)
  const [enabled, setEnabled] = useState(true);
  const [logDeletions, setLogDeletions] = useState(false);
  const [logChannel, setLogChannel] = useState("");
  const [hasChanges, setHasChanges] = useState(false);

  // Real data from API
  const [channels, setChannels] = useState<PurgeConfig[]>([]);

  useEffect(() => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<PurgeConfig[]>(`/api/guilds/${guildId}/config/autopurge`)
      .then((data) => {
        setChannels(data);
      })
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, [guildId]);

  const handleChange = (setter: Function) => (value: any) => {
    setter(value);
    setHasChanges(true);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="w-8 h-8 border-4 border-[var(--accent)] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (error && channels.length === 0) {
    return (
      <div className="text-center py-20">
        <p className="text-red-400 mb-2">Failed to load auto-purge config</p>
        <p className="text-sm text-[var(--muted)]">{error}</p>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Auto Purge</h1>
      <p className="text-[var(--muted)] mb-6">
        Automatically delete messages in specified channels on a schedule to keep your server clean.
      </p>

      {error && (
        <div className="mb-4 p-3 rounded-lg bg-red-500/10 border border-red-500/30 text-red-400 text-sm">
          {error}
        </div>
      )}

      <div className="space-y-6">
        <ConfigPanel
          title="Auto Purge Settings"
          description="These settings are local-only for now. Use bot commands to configure purge channels."
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
            <p className="text-[var(--muted)] text-sm">
              No auto-purge channels configured. Use <code className="text-[var(--accent)]">.autopurge</code> bot commands to add channels.
            </p>
          ) : (
            <div className="space-y-3">
              {channels.map((ch) => (
                <div
                  key={ch.id}
                  className="flex items-center justify-between p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]"
                >
                  <div>
                    <p className="text-sm font-medium">Channel {ch.channelId}</p>
                    <p className="text-xs text-[var(--muted)]">
                      Every {ch.intervalHours}h &middot; Max message age: {ch.maxMessageAgeHours}h
                    </p>
                  </div>
                  <span className="text-xs px-2 py-1 rounded-full bg-green-500/20 text-green-400">
                    Active
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
