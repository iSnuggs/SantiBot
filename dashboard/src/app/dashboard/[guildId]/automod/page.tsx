"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface AutomodRule {
  id: number;
  enabled: boolean;
  filterType: string;
  action: string;
  actionDurationMinutes: number | null;
  threshold: number;
  timeWindowSeconds: number;
  patternOrList: string | null;
  customResponseText: string | null;
}

const actionColors: Record<string, string> = {
  Delete: "bg-blue-500/20 text-blue-400 border-blue-500/30",
  Warn: "bg-yellow-500/20 text-yellow-400 border-yellow-500/30",
  Mute: "bg-orange-500/20 text-orange-400 border-orange-500/30",
  Kick: "bg-red-500/20 text-red-400 border-red-500/30",
  Ban: "bg-red-500/20 text-red-400 border-red-500/30",
};

export default function AutomodPage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [rules, setRules] = useState<AutomodRule[]>([]);

  const fetchData = () => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<AutomodRule[]>(`/api/guilds/${guildId}/config/automod`)
      .then((data) => setRules(data))
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
        <p className="text-red-400 mb-2">Failed to load automod rules</p>
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
      <h1 className="text-2xl font-bold mb-6">Auto-Moderation</h1>
      <p className="text-[var(--muted)] mb-6">
        View the auto-moderation rules configured for this server. Rules are managed
        via bot commands.
      </p>

      {rules.length === 0 ? (
        <div className="bg-[var(--card)] rounded-xl border border-[var(--border)] p-8 text-center">
          <p className="text-[var(--muted)]">
            No automod rules configured. Use <code className="text-[var(--accent)] bg-[var(--accent)]/10 px-1.5 py-0.5 rounded text-sm">.automod add</code> to create them.
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {rules.map((rule) => (
            <div
              key={rule.id}
              className="bg-[var(--card)] rounded-xl border border-[var(--border)] p-5"
            >
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-3">
                  <h3 className="text-lg font-semibold">{rule.filterType}</h3>
                  <span
                    className={`px-2 py-0.5 text-xs font-medium rounded-full border ${
                      rule.enabled
                        ? "bg-green-500/20 text-green-400 border-green-500/30"
                        : "bg-[var(--muted)]/10 text-[var(--muted)] border-[var(--border)]"
                    }`}
                  >
                    {rule.enabled ? "Enabled" : "Disabled"}
                  </span>
                </div>
                <span
                  className={`px-2.5 py-0.5 text-xs font-medium rounded-full border ${
                    actionColors[rule.action] || "bg-[var(--muted)]/10 text-[var(--muted)] border-[var(--border)]"
                  }`}
                >
                  {rule.action}
                  {rule.actionDurationMinutes ? ` (${rule.actionDurationMinutes}m)` : ""}
                </span>
              </div>

              <div className="flex flex-wrap gap-4 text-sm text-[var(--muted)]">
                <span>
                  Threshold: <span className="text-[var(--foreground)]">{rule.threshold}</span>
                </span>
                <span>
                  Time Window: <span className="text-[var(--foreground)]">{rule.timeWindowSeconds}s</span>
                </span>
              </div>

              {rule.patternOrList && (
                <div className="mt-3 p-3 bg-[var(--background)] rounded-lg border border-[var(--border)]">
                  <p className="text-xs text-[var(--muted)] mb-1">Pattern / Word List</p>
                  <p className="text-sm font-mono break-all">{rule.patternOrList}</p>
                </div>
              )}

              {rule.customResponseText && (
                <div className="mt-2 text-sm text-[var(--muted)]">
                  Response: <span className="text-[var(--foreground)] italic">{rule.customResponseText}</span>
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
