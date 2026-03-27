"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface AutoResponse {
  id: number;
  enabled: boolean;
  trigger: string;
  triggerType: string;
  responseText: string;
  responseType: string;
  deleteTrigger: boolean;
  userCooldownSeconds: number;
  channelCooldownSeconds: number;
}

const triggerTypeColors: Record<string, string> = {
  Contains: "bg-blue-500/20 text-blue-400 border-blue-500/30",
  ExactMatch: "bg-purple-500/20 text-purple-400 border-purple-500/30",
  StartsWith: "bg-cyan-500/20 text-cyan-400 border-cyan-500/30",
  EndsWith: "bg-teal-500/20 text-teal-400 border-teal-500/30",
  Regex: "bg-orange-500/20 text-orange-400 border-orange-500/30",
};

const responseTypeColors: Record<string, string> = {
  Text: "bg-[var(--muted)]/15 text-[var(--foreground)] border-[var(--border)]",
  Embed: "bg-indigo-500/20 text-indigo-400 border-indigo-500/30",
  DM: "bg-yellow-500/20 text-yellow-400 border-yellow-500/30",
  Reaction: "bg-pink-500/20 text-pink-400 border-pink-500/30",
};

export default function AutoResponderPage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [responses, setResponses] = useState<AutoResponse[]>([]);

  const fetchData = () => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<AutoResponse[]>(`/api/guilds/${guildId}/config/autoresponder`)
      .then((data) => setResponses(data))
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
        <p className="text-red-400 mb-2">Failed to load auto-responses</p>
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
      <h1 className="text-2xl font-bold mb-6">Auto-Responder</h1>
      <p className="text-[var(--muted)] mb-6">
        View the automatic responses configured for this server. Auto-responses are
        managed via bot commands.
      </p>

      {responses.length === 0 ? (
        <div className="bg-[var(--card)] rounded-xl border border-[var(--border)] p-8 text-center">
          <p className="text-[var(--muted)]">
            No auto-responses configured. Use <code className="text-[var(--accent)] bg-[var(--accent)]/10 px-1.5 py-0.5 rounded text-sm">.autoresponse add</code> to create them.
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {responses.map((ar) => (
            <div
              key={ar.id}
              className="bg-[var(--card)] rounded-xl border border-[var(--border)] p-5"
            >
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-3 min-w-0">
                  <code className="text-[var(--accent)] bg-[var(--accent)]/10 px-2 py-1 rounded text-sm font-mono truncate max-w-xs">
                    {ar.trigger}
                  </code>
                  <span
                    className={`px-2 py-0.5 text-xs font-medium rounded-full border shrink-0 ${
                      triggerTypeColors[ar.triggerType] || "bg-[var(--muted)]/10 text-[var(--muted)] border-[var(--border)]"
                    }`}
                  >
                    {ar.triggerType}
                  </span>
                </div>
                <span
                  className={`px-2 py-0.5 text-xs font-medium rounded-full border shrink-0 ${
                    ar.enabled
                      ? "bg-green-500/20 text-green-400 border-green-500/30"
                      : "bg-[var(--muted)]/10 text-[var(--muted)] border-[var(--border)]"
                  }`}
                >
                  {ar.enabled ? "Enabled" : "Disabled"}
                </span>
              </div>

              <div className="mb-3 p-3 bg-[var(--background)] rounded-lg border border-[var(--border)]">
                <div className="flex items-center gap-2 mb-1">
                  <p className="text-xs text-[var(--muted)]">Response</p>
                  <span
                    className={`px-1.5 py-0.5 text-[10px] font-medium rounded-full border ${
                      responseTypeColors[ar.responseType] || "bg-[var(--muted)]/10 text-[var(--muted)] border-[var(--border)]"
                    }`}
                  >
                    {ar.responseType}
                  </span>
                </div>
                <p className="text-sm">
                  {ar.responseText.length > 100
                    ? ar.responseText.slice(0, 100) + "..."
                    : ar.responseText}
                </p>
              </div>

              <div className="flex flex-wrap gap-4 text-sm text-[var(--muted)]">
                {ar.deleteTrigger && (
                  <span className="text-yellow-400">Deletes trigger message</span>
                )}
                {ar.userCooldownSeconds > 0 && (
                  <span>
                    User cooldown: <span className="text-[var(--foreground)]">{ar.userCooldownSeconds}s</span>
                  </span>
                )}
                {ar.channelCooldownSeconds > 0 && (
                  <span>
                    Channel cooldown: <span className="text-[var(--foreground)]">{ar.channelCooldownSeconds}s</span>
                  </span>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
