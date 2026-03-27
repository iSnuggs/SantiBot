"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface WelcomeConfig {
  configured: boolean;
  enabled: boolean;
  channelId: string | null;
  backgroundUrl: string | null;
  accentColor: string | null;
  welcomeText: string | null;
  subtitleText: string | null;
}

export default function WelcomePage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [config, setConfig] = useState<WelcomeConfig | null>(null);

  const fetchData = () => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<WelcomeConfig>(`/api/guilds/${guildId}/config/welcome`)
      .then((data) => setConfig(data))
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    fetchData();
  }, [guildId]);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="w-8 h-8 border-4 border-[var(--accent)] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-20">
        <p className="text-red-400 mb-2">Failed to load welcome config</p>
        <p className="text-sm text-[var(--muted)] mb-4">{error}</p>
        <button
          onClick={fetchData}
          className="px-4 py-2 text-sm rounded-lg bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white transition-colors"
        >
          Retry
        </button>
      </div>
    );
  }

  if (!config || !config.configured) {
    return (
      <div>
        <h1 className="text-2xl font-bold mb-6">Welcome</h1>
        <div className="bg-[var(--card)] rounded-xl border border-[var(--border)] p-6">
          <p className="text-[var(--muted)]">
            Welcome images not set up. Use <code className="px-1.5 py-0.5 bg-[var(--background)] rounded text-sm">.welcomeimg</code> to configure.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Welcome</h1>
      <p className="text-[var(--muted)] mb-6">
        View your server's welcome image settings. Use bot commands to make changes.
      </p>

      <ConfigPanel title="Welcome Settings">
        <div className="space-y-4">
          {/* Enabled Status */}
          <div className="flex items-center justify-between py-2">
            <span className="text-sm font-medium">Status</span>
            <span
              className={`px-2.5 py-0.5 text-xs font-medium rounded-full ${
                config.enabled
                  ? "bg-green-500/15 text-green-400"
                  : "bg-red-500/15 text-red-400"
              }`}
            >
              {config.enabled ? "Enabled" : "Disabled"}
            </span>
          </div>

          {/* Welcome Channel ID */}
          <div className="flex items-center justify-between py-2 border-t border-[var(--border)]">
            <span className="text-sm font-medium">Welcome Channel ID</span>
            <span className="text-sm text-[var(--muted)]">
              {config.channelId || "Not set"}
            </span>
          </div>

          {/* Welcome Text */}
          <div className="flex items-center justify-between py-2 border-t border-[var(--border)]">
            <span className="text-sm font-medium">Welcome Text</span>
            <span className="text-sm text-[var(--muted)] max-w-[60%] text-right">
              {config.welcomeText || "Not set"}
            </span>
          </div>

          {/* Subtitle Text */}
          <div className="flex items-center justify-between py-2 border-t border-[var(--border)]">
            <span className="text-sm font-medium">Subtitle Text</span>
            <span className="text-sm text-[var(--muted)] max-w-[60%] text-right">
              {config.subtitleText || "Not set"}
            </span>
          </div>

          {/* Background URL */}
          <div className="flex items-center justify-between py-2 border-t border-[var(--border)]">
            <span className="text-sm font-medium">Background URL</span>
            <span className="text-sm text-[var(--muted)] max-w-[60%] text-right truncate">
              {config.backgroundUrl || "Not set"}
            </span>
          </div>

          {/* Accent Color */}
          <div className="flex items-center justify-between py-2 border-t border-[var(--border)]">
            <span className="text-sm font-medium">Accent Color</span>
            {config.accentColor ? (
              <div className="flex items-center gap-2">
                <div
                  className="w-5 h-5 rounded border border-[var(--border)]"
                  style={{ backgroundColor: config.accentColor }}
                />
                <span className="text-sm text-[var(--muted)] font-mono">
                  {config.accentColor}
                </span>
              </div>
            ) : (
              <span className="text-sm text-[var(--muted)]">Not set</span>
            )}
          </div>
        </div>
      </ConfigPanel>
    </div>
  );
}
