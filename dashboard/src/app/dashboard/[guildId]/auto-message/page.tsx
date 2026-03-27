"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface AutoMessage {
  id: number;
  channelId: string;
  content: string;
  isRecurring: boolean;
  scheduledAt: string;
  interval: string | null;
  lastSentAt: string | null;
  isActive: boolean;
  creatorUserId: string;
}

export default function AutoMessagePage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [messages, setMessages] = useState<AutoMessage[]>([]);

  const fetchData = () => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<AutoMessage[]>(`/api/guilds/${guildId}/config/automessage`)
      .then((data) => setMessages(data))
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
        <p className="text-red-400 mb-2">Failed to load auto-message config</p>
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
      <h1 className="text-2xl font-bold mb-6">Auto Message</h1>
      <p className="text-[var(--muted)] mb-6">
        View scheduled messages that SantiBot automatically sends to your channels.
      </p>

      <ConfigPanel title="Scheduled Messages" description="Read-only overview of all scheduled auto-messages.">
        {messages.length === 0 ? (
          <p className="text-[var(--muted)] text-sm py-4">
            No scheduled messages. Use <code className="px-1.5 py-0.5 bg-[var(--background)] border border-[var(--border)] rounded text-xs">.schedule</code> to create them.
          </p>
        ) : (
          <div className="space-y-4">
            {messages.map((msg) => (
              <div
                key={msg.id}
                className="p-4 bg-[var(--background)] border border-[var(--border)] rounded-lg"
              >
                <div className="flex items-start justify-between gap-4 mb-3">
                  <p className="text-sm flex-1">
                    {msg.content.length > 100
                      ? msg.content.slice(0, 100) + "..."
                      : msg.content}
                  </p>
                  <div className="flex gap-2 shrink-0">
                    <span
                      className={`px-2 py-0.5 text-xs rounded-full font-medium ${
                        msg.isRecurring
                          ? "bg-green-500/15 text-green-400"
                          : "bg-[var(--muted)]/15 text-[var(--muted)]"
                      }`}
                    >
                      {msg.isRecurring ? "Recurring" : "One-time"}
                    </span>
                    <span
                      className={`px-2 py-0.5 text-xs rounded-full font-medium ${
                        msg.isActive
                          ? "bg-green-500/15 text-green-400"
                          : "bg-red-500/15 text-red-400"
                      }`}
                    >
                      {msg.isActive ? "Active" : "Inactive"}
                    </span>
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-2 text-xs text-[var(--muted)]">
                  <div>
                    <span className="font-medium">Channel:</span> {msg.channelId}
                  </div>
                  {msg.isRecurring && msg.interval && (
                    <div>
                      <span className="font-medium">Interval:</span> {msg.interval}
                    </div>
                  )}
                  <div>
                    <span className="font-medium">Next Send:</span>{" "}
                    {new Date(msg.scheduledAt).toLocaleString()}
                  </div>
                  {msg.lastSentAt && (
                    <div>
                      <span className="font-medium">Last Sent:</span>{" "}
                      {new Date(msg.lastSentAt).toLocaleString()}
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </ConfigPanel>
    </div>
  );
}
