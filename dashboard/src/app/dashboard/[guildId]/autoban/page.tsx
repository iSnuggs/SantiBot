"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface AutobanRule {
  id: number;
  enabled: boolean;
  ruleType: "AccountAge" | "Username" | "NoAvatar";
  minAccountAgeHours: number | null;
  usernamePatterns: string[] | null;
  action: string;
  reason: string | null;
}

export default function AutobanPage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [rules, setRules] = useState<AutobanRule[]>([]);

  const fetchData = () => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<AutobanRule[]>(`/api/guilds/${guildId}/config/autoban`)
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
        <p className="text-red-400 mb-2">Failed to load autoban rules</p>
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
      <h1 className="text-2xl font-bold mb-6">Autoban</h1>
      <p className="text-[var(--muted)] mb-6">
        Automatically ban accounts that match configured rules. Protect your server from raids and spam accounts.
      </p>

      {rules.length > 0 ? (
        <div className="space-y-4">
          {rules.map((rule) => (
            <ConfigPanel key={rule.id} title={rule.ruleType}>
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
                  <span className="text-xs px-2 py-1 rounded-full border bg-[var(--accent)]/20 text-[var(--accent)] border-[var(--accent)]/30">
                    {rule.action}
                  </span>
                </div>

                <div className="text-sm">
                  {rule.ruleType === "AccountAge" && rule.minAccountAgeHours != null && (
                    <p>
                      <span className="text-[var(--muted)]">Min age:</span>{" "}
                      {rule.minAccountAgeHours} hours
                    </p>
                  )}
                  {rule.ruleType === "Username" && rule.usernamePatterns && (
                    <div>
                      <span className="text-[var(--muted)] block mb-1">Patterns:</span>
                      <div className="flex flex-wrap gap-1">
                        {rule.usernamePatterns.map((pattern, i) => (
                          <code
                            key={i}
                            className="bg-[var(--background)] px-2 py-0.5 rounded text-xs border border-[var(--border)]"
                          >
                            {pattern}
                          </code>
                        ))}
                      </div>
                    </div>
                  )}
                  {rule.ruleType === "NoAvatar" && (
                    <p className="text-[var(--muted)]">
                      Bans accounts with default avatar
                    </p>
                  )}
                </div>

                {rule.reason && (
                  <div className="pt-2 border-t border-[var(--border)]">
                    <span className="text-xs text-[var(--muted)] block mb-1">Reason</span>
                    <p className="text-sm">{rule.reason}</p>
                  </div>
                )}
              </div>
            </ConfigPanel>
          ))}
        </div>
      ) : (
        <ConfigPanel title="Autoban Rules">
          <p className="text-[var(--muted)] text-sm">
            No autoban rules configured. Use{" "}
            <code className="bg-[var(--background)] px-1.5 py-0.5 rounded text-xs">.autoban</code>{" "}
            commands to create them.
          </p>
        </ConfigPanel>
      )}
    </div>
  );
}
