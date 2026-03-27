"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface VoiceTextLink {
  id: number;
  voiceChannelId: string;
  textChannelId: string;
}

interface VoiceTextConfig {
  links: VoiceTextLink[];
  dehoist: {
    enabled: boolean;
    replacementPrefix: string;
  };
}

export default function VoiceTextPage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [config, setConfig] = useState<VoiceTextConfig | null>(null);

  const fetchData = () => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<VoiceTextConfig>(`/api/guilds/${guildId}/config/voicetext`)
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
        <p className="text-red-400 mb-2">Failed to load voice-text config</p>
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

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Voice & Text</h1>
      <p className="text-[var(--muted)] mb-6">
        Manage voice-text channel links and display name dehoisting settings.
      </p>

      <div className="space-y-6">
        <ConfigPanel title="Voice-Text Links" description="Voice channels linked to text channels for automatic access.">
          {!config?.links.length ? (
            <p className="text-[var(--muted)] text-sm py-4">
              No voice-text links. Use <code className="px-1.5 py-0.5 bg-[var(--background)] border border-[var(--border)] rounded text-xs">.vt link</code> to create them.
            </p>
          ) : (
            <div className="space-y-3">
              {config.links.map((link) => (
                <div
                  key={link.id}
                  className="flex items-center gap-3 p-3 bg-[var(--background)] border border-[var(--border)] rounded-lg text-sm"
                >
                  <div className="flex-1">
                    <span className="font-medium">Voice:</span>{" "}
                    <span className="text-[var(--muted)]">{link.voiceChannelId}</span>
                  </div>
                  <svg
                    className="w-4 h-4 text-[var(--muted)] shrink-0"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                    strokeWidth={2}
                  >
                    <path strokeLinecap="round" strokeLinejoin="round" d="M13 7l5 5m0 0l-5 5m5-5H6" />
                  </svg>
                  <div className="flex-1">
                    <span className="font-medium">Text:</span>{" "}
                    <span className="text-[var(--muted)]">{link.textChannelId}</span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </ConfigPanel>

        <ConfigPanel title="Auto-Dehoist" description="Automatically fix display names that start with special characters to push to the top of the member list.">
          <div className="space-y-4">
            <div className="flex items-center justify-between py-2">
              <span className="text-sm font-medium">Status</span>
              <span
                className={`px-3 py-1 text-sm rounded-full font-semibold ${
                  config?.dehoist.enabled
                    ? "bg-green-500/15 text-green-400"
                    : "bg-red-500/15 text-red-400"
                }`}
              >
                {config?.dehoist.enabled ? "Enabled" : "Disabled"}
              </span>
            </div>

            <div className="py-2 border-t border-[var(--border)]">
              <span className="text-sm font-medium block mb-1">Replacement Prefix</span>
              <span className="text-sm text-[var(--muted)]">
                {config?.dehoist.replacementPrefix ? (
                  <code className="px-1.5 py-0.5 bg-[var(--background)] border border-[var(--border)] rounded text-xs">
                    {config.dehoist.replacementPrefix}
                  </code>
                ) : (
                  "Not set"
                )}
              </span>
            </div>
          </div>
        </ConfigPanel>
      </div>
    </div>
  );
}
