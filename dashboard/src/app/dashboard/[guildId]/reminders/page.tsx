"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface Reminder {
  id: string;
  message: string;
  when: string;
  channelId: string;
  userId: string;
  isPrivate: boolean;
}

function formatDate(dateStr: string): string {
  const date = new Date(dateStr);
  return date.toLocaleString(undefined, {
    weekday: "short",
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function truncate(text: string, max: number): string {
  return text.length > max ? text.slice(0, max) + "..." : text;
}

export default function RemindersPage() {
  const { guildId } = useParams();
  const [reminders, setReminders] = useState<Reminder[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchReminders = () => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<Reminder[]>(`/api/guilds/${guildId}/config/reminders`)
      .then((data) => {
        setReminders(data);
        setError(null);
      })
      .catch((err) => {
        setError(err.message || "Failed to load reminders");
      })
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    fetchReminders();
  }, [guildId]);

  const isUpcoming = (when: string) => new Date(when) > new Date();

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Reminders</h1>
      <p className="text-[var(--muted)] mb-6">
        View all reminders set in this server. Reminders are created via bot commands and will fire at
        the scheduled time.
      </p>

      <div className="space-y-6">
        <ConfigPanel title="Reminder List" description="Read-only view of all server reminders">
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <div className="w-6 h-6 border-2 border-[var(--accent)] border-t-transparent rounded-full animate-spin" />
              <span className="ml-3 text-sm text-[var(--muted)]">Loading reminders...</span>
            </div>
          ) : error ? (
            <div className="text-center py-8">
              <p className="text-red-400 text-sm">{error}</p>
              <button
                onClick={fetchReminders}
                className="mt-3 px-4 py-2 text-sm rounded-lg bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white transition-colors"
              >
                Retry
              </button>
            </div>
          ) : reminders.length === 0 ? (
            <p className="text-[var(--muted)] text-sm">
              No reminders set for this server.
            </p>
          ) : (
            <div className="space-y-3">
              {reminders.map((r) => (
                <div
                  key={r.id}
                  className="flex items-center justify-between p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]"
                >
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-medium truncate">
                      {truncate(r.message, 100)}
                    </p>
                    <p className="text-xs text-[var(--muted)] mt-1">
                      {formatDate(r.when)} &middot; Channel: {r.channelId}
                    </p>
                  </div>
                  <div className="flex items-center gap-2 ml-3 shrink-0">
                    {r.isPrivate && (
                      <span className="text-xs px-2 py-1 rounded-full bg-purple-500/20 text-purple-400">
                        Private
                      </span>
                    )}
                    {isUpcoming(r.when) ? (
                      <span className="text-xs px-2 py-1 rounded-full bg-green-500/20 text-green-400">
                        Upcoming
                      </span>
                    ) : (
                      <span className="text-xs px-2 py-1 rounded-full bg-[var(--border)] text-[var(--muted)]">
                        Past
                      </span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </ConfigPanel>
      </div>
    </div>
  );
}
