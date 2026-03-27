"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface Suggestion {
  id: number;
  authorId: string;
  content: string;
  status: string;
  statusReason: string | null;
  channelId: string;
  messageId: string;
}

const STATUS_COLORS: Record<string, string> = {
  Pending: "bg-blue-500/15 text-blue-400",
  Approved: "bg-green-500/15 text-green-400",
  Denied: "bg-red-500/15 text-red-400",
  Implemented: "bg-purple-500/15 text-purple-400",
};

function getStatusColor(status: string): string {
  return STATUS_COLORS[status] || "bg-[var(--muted)]/15 text-[var(--muted)]";
}

export default function SuggestionsPage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [suggestions, setSuggestions] = useState<Suggestion[]>([]);

  const fetchData = () => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<Suggestion[]>(`/api/guilds/${guildId}/config/suggestions`)
      .then((data) => setSuggestions(data))
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
        <p className="text-red-400 mb-2">Failed to load suggestions</p>
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
      <h1 className="text-2xl font-bold mb-6">Suggestions</h1>
      <p className="text-[var(--muted)] mb-6">
        View community suggestions submitted through the bot.
      </p>

      <ConfigPanel title="Suggestions">
        {suggestions.length === 0 ? (
          <p className="text-[var(--muted)] text-sm">No suggestions yet.</p>
        ) : (
          <div className="space-y-3">
            {suggestions.map((s) => (
              <div
                key={s.id}
                className="p-4 rounded-lg bg-[var(--background)] border border-[var(--border)]"
              >
                {/* Header row: status badge + author */}
                <div className="flex items-center justify-between mb-2">
                  <span
                    className={`px-2.5 py-0.5 text-xs font-medium rounded-full ${getStatusColor(
                      s.status
                    )}`}
                  >
                    {s.status}
                  </span>
                  <span className="text-xs text-[var(--muted)]">
                    Author: {s.authorId}
                  </span>
                </div>

                {/* Content */}
                <p className="text-sm">{s.content}</p>

                {/* Status reason */}
                {s.statusReason && (
                  <p className="text-xs text-[var(--muted)] mt-2 italic">
                    Reason: {s.statusReason}
                  </p>
                )}
              </div>
            ))}
          </div>
        )}
      </ConfigPanel>
    </div>
  );
}
