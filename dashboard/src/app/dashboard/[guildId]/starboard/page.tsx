"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel, { Toggle, InputField } from "@/components/ConfigPanel";

interface StarboardConfig {
  enabled: boolean;
  channelId: number | null;
  threshold: number;
  emoji: string;
  allowSelfStar: boolean;
}

export default function StarboardPage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const [enabled, setEnabled] = useState(false);
  const [threshold, setThreshold] = useState("3");
  const [selfStar, setSelfStar] = useState(false);
  const [channelId, setChannelId] = useState("");
  const [emoji, setEmoji] = useState("⭐");
  const [hasChanges, setHasChanges] = useState(false);

  // Snapshot of last-saved values for discard
  const [saved, setSaved] = useState({ enabled: false, threshold: "3", selfStar: false, channelId: "", emoji: "⭐" });

  useEffect(() => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<StarboardConfig>(`/api/guilds/${guildId}/config/starboard`)
      .then((data) => {
        const state = {
          enabled: data.enabled,
          threshold: String(data.threshold),
          selfStar: data.allowSelfStar,
          channelId: data.channelId ? String(data.channelId) : "",
          emoji: data.emoji || "⭐",
        };
        setEnabled(state.enabled);
        setThreshold(state.threshold);
        setSelfStar(state.selfStar);
        setChannelId(state.channelId);
        setEmoji(state.emoji);
        setSaved(state);
      })
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, [guildId]);

  const handleChange = (setter: Function) => (value: any) => {
    setter(value);
    setHasChanges(true);
  };

  const handleSave = async () => {
    if (!guildId) return;
    setSaving(true);
    setError(null);
    try {
      await apiFetch(`/api/guilds/${guildId}/config/starboard`, {
        method: "PATCH",
        body: JSON.stringify({
          enabled,
          channelId: channelId ? Number(channelId) : null,
          threshold: Number(threshold),
          emoji,
          allowSelfStar: selfStar,
        }),
      });
      const newSaved = { enabled, threshold, selfStar, channelId, emoji };
      setSaved(newSaved);
      setHasChanges(false);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleDiscard = () => {
    setEnabled(saved.enabled);
    setThreshold(saved.threshold);
    setSelfStar(saved.selfStar);
    setChannelId(saved.channelId);
    setEmoji(saved.emoji);
    setHasChanges(false);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="w-8 h-8 border-4 border-[var(--accent)] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (error && !enabled && !channelId) {
    return (
      <div className="text-center py-20">
        <p className="text-red-400 mb-2">Failed to load starboard config</p>
        <p className="text-sm text-[var(--muted)]">{error}</p>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Starboard</h1>
      <p className="text-[var(--muted)] mb-6">
        Highlight popular messages by collecting star reactions.
        When a message receives enough stars, it gets posted to the starboard channel.
      </p>

      {error && (
        <div className="mb-4 p-3 rounded-lg bg-red-500/10 border border-red-500/30 text-red-400 text-sm">
          {error}
        </div>
      )}

      <ConfigPanel
        title="Starboard Settings"
        hasChanges={hasChanges}
        onSave={handleSave}
        onDiscard={handleDiscard}
      >
        <Toggle
          label="Enable Starboard"
          checked={enabled}
          onChange={handleChange(setEnabled)}
        />
        <InputField
          label="Starboard Channel ID"
          value={channelId}
          onChange={handleChange(setChannelId)}
          placeholder="Enter channel ID"
        />
        <InputField
          label="Star Threshold"
          value={threshold}
          onChange={handleChange(setThreshold)}
          placeholder="3"
          type="number"
        />
        <Toggle
          label="Allow Self-Starring"
          checked={selfStar}
          onChange={handleChange(setSelfStar)}
        />
      </ConfigPanel>
    </div>
  );
}
