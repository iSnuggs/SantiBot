"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface AutoDeleteRule {
  id: number;
  channelId: string;
  enabled: boolean;
  delaySeconds: number;
  useFilter: boolean;
  filter: string | null;
  ignorePinned: boolean;
}

export default function AutoDeletePage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [rules, setRules] = useState<AutoDeleteRule[]>([]);

  const fetchData = () => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<AutoDeleteRule[]>(`/api/guilds/${guildId}/config/autodelete`)
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
        <p className="text-red-400 mb-2">Failed to load auto-delete rules</p>
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
      <h1 className="text-2xl font-bold mb-6">Auto Delete</h1>
      <p className="text-[var(--muted)] mb-6">
        Automatically delete messages in specific channels after a set delay. Useful for keeping temporary channels clean.
      </p>

      {rules.length > 0 ? (
        <div className="space-y-4">
          {rules.map((rule) => (
            <ConfigPanel key={rule.id} title={`Channel ${rule.channelId}`}>
              <div className="space-y-3">
                <div className="flex items-center gap-2 flex-wrap">
                  <span
                    className={`text-xs px-2 py-1 rounded-full border ${
                      rule.enabled
                        ? "bg-green-500/20 text-green-400 border-green-500/30"
                        : "bg-gray-500/20 text-gray-400 border-gray-500/30"
                    }`}
                  >
                    {rule.enabled ? "Enabled" : "Disabled"}
                  </span>
                  {rule.ignorePinned && (
                    <span className="text-xs px-2 py-1 rounded-full border bg-blue-500/20 text-blue-400 border-blue-500/30">
                      Ignores Pinned
                    </span>
                  )}
                </div>

                <div className="space-y-2 text-sm">
                  <div className="flex items-center justify-between py-2">
                    <span className="text-[var(--muted)]">Channel ID</span>
                    <span className="font-mono">{rule.channelId}</span>
                  </div>
                  <div className="flex items-center justify-between py-2 border-t border-[var(--border)]">
                    <span className="text-[var(--muted)]">Delay</span>
                    <span>{rule.delaySeconds} seconds</span>
                  </div>
                  {rule.useFilter && (
                    <div className="pt-2 border-t border-[var(--border)]">
                      <span className="text-[var(--muted)] block mb-1">Filter</span>
                      <code className="bg-[var(--background)] px-2 py-1 rounded text-xs border border-[var(--border)] block">
                        {rule.filter || "No filter pattern"}
                      </code>
                    </div>
                  )}
                </div>
              </div>
            </ConfigPanel>
          ))}
        </div>
      ) : (
        <ConfigPanel title="Auto Delete Rules">
          <p className="text-[var(--muted)] text-sm">
            No auto-delete rules configured. Use{" "}
            <code className="bg-[var(--background)] px-1.5 py-0.5 rounded text-xs">.autodelete add</code>{" "}
            to create them.
          </p>
        </ConfigPanel>
      )}
    </div>
  );
}
