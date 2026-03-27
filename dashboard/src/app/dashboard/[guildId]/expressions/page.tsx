"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface Expression {
  id: string;
  trigger: string;
  response: string;
  autoDeleteTrigger: boolean;
  dmResponse: boolean;
  containsAnywhere: boolean;
  allowTarget: boolean;
}

export default function ExpressionsPage() {
  const { guildId } = useParams();
  const [expressions, setExpressions] = useState<Expression[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!guildId) return;
    setLoading(true);
    apiFetch<Expression[]>(`/api/guilds/${guildId}/config/expressions`)
      .then((data) => {
        setExpressions(data);
        setError(null);
      })
      .catch((err) => {
        setError(err.message || "Failed to load expressions");
      })
      .finally(() => setLoading(false));
  }, [guildId]);

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Custom Expressions</h1>
      <p className="text-[var(--muted)] mb-6">
        Set up automatic responses triggered by specific words or patterns in messages.
      </p>

      <div className="space-y-6">
        <ConfigPanel title="Expression List">
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <div className="w-6 h-6 border-2 border-[var(--accent)] border-t-transparent rounded-full animate-spin" />
              <span className="ml-3 text-sm text-[var(--muted)]">Loading expressions...</span>
            </div>
          ) : error ? (
            <div className="text-center py-8">
              <p className="text-red-400 text-sm">{error}</p>
              <button
                onClick={() => {
                  setLoading(true);
                  setError(null);
                  apiFetch<Expression[]>(`/api/guilds/${guildId}/config/expressions`)
                    .then((data) => setExpressions(data))
                    .catch((err) => setError(err.message || "Failed to load expressions"))
                    .finally(() => setLoading(false));
                }}
                className="mt-3 px-4 py-2 text-sm rounded-lg bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white transition-colors"
              >
                Retry
              </button>
            </div>
          ) : expressions.length === 0 ? (
            <p className="text-[var(--muted)] text-sm">
              No expressions configured yet. Use bot commands to create them.
            </p>
          ) : (
            <div className="space-y-3">
              {expressions.map((expr) => (
                <div
                  key={expr.id}
                  className="p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]"
                >
                  <div className="flex items-center justify-between mb-1">
                    <code className="text-sm font-mono text-[var(--accent)]">{expr.trigger}</code>
                    <div className="flex gap-1.5">
                      {expr.containsAnywhere && (
                        <span className="text-xs px-2 py-0.5 rounded-full bg-[var(--border)] text-[var(--muted)]">
                          Contains Anywhere
                        </span>
                      )}
                      {expr.autoDeleteTrigger && (
                        <span className="text-xs px-2 py-0.5 rounded-full bg-[var(--border)] text-[var(--muted)]">
                          Auto-Delete
                        </span>
                      )}
                      {expr.dmResponse && (
                        <span className="text-xs px-2 py-0.5 rounded-full bg-[var(--border)] text-[var(--muted)]">
                          DM
                        </span>
                      )}
                    </div>
                  </div>
                  <p className="text-xs text-[var(--muted)]">{expr.response}</p>
                </div>
              ))}
            </div>
          )}
        </ConfigPanel>
      </div>
    </div>
  );
}
