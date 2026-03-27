"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface Poll {
  id: string;
  question: string;
  channelId: string;
  endsAt: string;
}

export default function PollsPage() {
  const { guildId } = useParams();
  const [polls, setPolls] = useState<Poll[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!guildId) return;
    setLoading(true);
    apiFetch<Poll[]>(`/api/guilds/${guildId}/config/polls`)
      .then((data) => {
        setPolls(data);
        setError(null);
      })
      .catch((err) => {
        setError(err.message || "Failed to load polls");
      })
      .finally(() => setLoading(false));
  }, [guildId]);

  const isActive = (endsAt: string) => new Date(endsAt) > new Date();

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Polls</h1>
      <p className="text-[var(--muted)] mb-6">
        Create and manage polls for your server. Let members vote on topics and view real-time results.
      </p>

      <div className="space-y-6">
        <ConfigPanel title="Poll List">
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <div className="w-6 h-6 border-2 border-[var(--accent)] border-t-transparent rounded-full animate-spin" />
              <span className="ml-3 text-sm text-[var(--muted)]">Loading polls...</span>
            </div>
          ) : error ? (
            <div className="text-center py-8">
              <p className="text-red-400 text-sm">{error}</p>
              <button
                onClick={() => {
                  setLoading(true);
                  setError(null);
                  apiFetch<Poll[]>(`/api/guilds/${guildId}/config/polls`)
                    .then((data) => setPolls(data))
                    .catch((err) => setError(err.message || "Failed to load polls"))
                    .finally(() => setLoading(false));
                }}
                className="mt-3 px-4 py-2 text-sm rounded-lg bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white transition-colors"
              >
                Retry
              </button>
            </div>
          ) : polls.length === 0 ? (
            <p className="text-[var(--muted)] text-sm">
              No active polls. Create them with bot commands.
            </p>
          ) : (
            <div className="space-y-3">
              {polls.map((p) => (
                <div
                  key={p.id}
                  className="flex items-center justify-between p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]"
                >
                  <div>
                    <p className="text-sm font-medium">{p.question}</p>
                    <p className="text-xs text-[var(--muted)]">
                      Ends {new Date(p.endsAt).toLocaleDateString()} &middot; Channel: {p.channelId}
                    </p>
                  </div>
                  {isActive(p.endsAt) ? (
                    <span className="text-xs px-2 py-1 rounded-full bg-green-500/20 text-green-400">
                      Active
                    </span>
                  ) : (
                    <span className="text-xs px-2 py-1 rounded-full bg-[var(--border)] text-[var(--muted)]">
                      Ended
                    </span>
                  )}
                </div>
              ))}
            </div>
          )}
        </ConfigPanel>
      </div>
    </div>
  );
}
