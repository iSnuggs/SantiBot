"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel, { Toggle, InputField, SelectField } from "@/components/ConfigPanel";

interface MusicConfig {
  configured: boolean;
  volume: number;
  autoDisconnect: boolean;
  autoPlay: boolean;
  repeat: string;
  quality: string;
  musicChannelId: number | null;
}

export default function MusicPage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const [volume, setVolume] = useState("50");
  const [autoDisconnect, setAutoDisconnect] = useState(true);
  const [autoPlay, setAutoPlay] = useState(false);
  const [repeat, setRepeat] = useState("off");
  const [quality, setQuality] = useState("high");
  const [musicChannelId, setMusicChannelId] = useState("");
  const [configured, setConfigured] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);

  // Snapshot for discard
  const [saved, setSaved] = useState({ volume: "50", autoDisconnect: true, autoPlay: false });

  useEffect(() => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<MusicConfig>(`/api/guilds/${guildId}/config/music`)
      .then((data) => {
        const state = {
          volume: String(data.volume),
          autoDisconnect: data.autoDisconnect,
          autoPlay: data.autoPlay,
        };
        setVolume(state.volume);
        setAutoDisconnect(state.autoDisconnect);
        setAutoPlay(state.autoPlay);
        setRepeat(data.repeat || "off");
        setQuality(data.quality || "high");
        setMusicChannelId(data.musicChannelId ? String(data.musicChannelId) : "");
        setConfigured(data.configured);
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
      await apiFetch(`/api/guilds/${guildId}/config/music`, {
        method: "PATCH",
        body: JSON.stringify({
          volume: Number(volume),
          autoDisconnect,
          autoPlay,
        }),
      });
      const newSaved = { volume, autoDisconnect, autoPlay };
      setSaved(newSaved);
      setHasChanges(false);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleDiscard = () => {
    setVolume(saved.volume);
    setAutoDisconnect(saved.autoDisconnect);
    setAutoPlay(saved.autoPlay);
    setHasChanges(false);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="w-8 h-8 border-4 border-[var(--accent)] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (error && !configured) {
    return (
      <div className="text-center py-20">
        <p className="text-red-400 mb-2">Failed to load music config</p>
        <p className="text-sm text-[var(--muted)]">{error}</p>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Music</h1>
      <p className="text-[var(--muted)] mb-6">
        Configure music playback settings, DJ restrictions, and queue behavior for your server.
      </p>

      {error && (
        <div className="mb-4 p-3 rounded-lg bg-red-500/10 border border-red-500/30 text-red-400 text-sm">
          {error}
        </div>
      )}

      <div className="space-y-6">
        <ConfigPanel
          title="Playback Settings"
          hasChanges={hasChanges}
          onSave={handleSave}
          onDiscard={handleDiscard}
        >
          <InputField
            label="Default Volume (%)"
            value={volume}
            onChange={handleChange(setVolume)}
            placeholder="50"
            type="number"
          />
          <Toggle
            label="Auto-Disconnect When Empty"
            checked={autoDisconnect}
            onChange={handleChange(setAutoDisconnect)}
          />
          <Toggle
            label="Auto-Play Recommended Tracks"
            checked={autoPlay}
            onChange={handleChange(setAutoPlay)}
          />
        </ConfigPanel>

        <ConfigPanel title="Read-Only Info">
          <div className="space-y-3">
            <div className="flex items-center justify-between py-2">
              <span className="text-sm">Quality</span>
              <span className="text-sm text-[var(--muted)] capitalize">{quality}</span>
            </div>
            <div className="flex items-center justify-between py-2">
              <span className="text-sm">Repeat Mode</span>
              <span className="text-sm text-[var(--muted)] capitalize">{repeat}</span>
            </div>
            {musicChannelId && (
              <div className="flex items-center justify-between py-2">
                <span className="text-sm">Music Channel</span>
                <span className="text-sm text-[var(--muted)]">{musicChannelId}</span>
              </div>
            )}
          </div>
        </ConfigPanel>
      </div>
    </div>
  );
}
