"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface StreamEntry {
  id: number;
  username: string;
  prettyName: string;
  type: string;
  channelId: string;
  message: string;
}

interface FeedEntry {
  id: number;
  url: string;
  channelId: string;
  message: string;
}

interface StreamsConfig {
  streams: StreamEntry[];
  feeds: FeedEntry[];
}

const TYPE_COLORS: Record<string, { bg: string; text: string }> = {
  twitch:  { bg: "bg-purple-500/20", text: "text-purple-400" },
  youtube: { bg: "bg-red-500/20",    text: "text-red-400" },
  kick:    { bg: "bg-green-500/20",  text: "text-green-400" },
};

function typeBadge(type: string) {
  const key = type.toLowerCase();
  const colors = TYPE_COLORS[key] || { bg: "bg-[var(--accent)]/20", text: "text-[var(--accent)]" };
  return (
    <span className={`px-2 py-0.5 rounded text-xs font-medium uppercase ${colors.bg} ${colors.text}`}>
      {type}
    </span>
  );
}

export default function StreamsPage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [config, setConfig] = useState<StreamsConfig>({ streams: [], feeds: [] });

  const fetchData = () => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<StreamsConfig>(`/api/guilds/${guildId}/config/streams`)
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
        <p className="text-red-400 mb-2">Failed to load streams config</p>
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
      <h1 className="text-2xl font-bold mb-6">Streams & Feeds</h1>
      <p className="text-[var(--muted)] mb-6">
        View the live-stream notifications and RSS feeds configured for this server.
      </p>

      <div className="space-y-6">
        {/* Followed Streams Panel */}
        <ConfigPanel title="Followed Streams">
          {config.streams.length === 0 ? (
            <p className="text-[var(--muted)] text-sm">
              No streams followed. Use <code className="px-1.5 py-0.5 bg-[var(--background)] rounded text-xs">.stream</code> commands to add them.
            </p>
          ) : (
            <div className="space-y-2">
              {config.streams.map((s) => (
                <div
                  key={s.id}
                  className="flex items-center gap-3 px-3 py-2.5 bg-[var(--background)] rounded-lg border border-[var(--border)]"
                >
                  <div className="flex items-center gap-2 min-w-0 flex-1">
                    {typeBadge(s.type)}
                    <span className="font-medium text-sm truncate">
                      {s.prettyName || s.username}
                    </span>
                    {s.prettyName && s.prettyName !== s.username && (
                      <span className="text-xs text-[var(--muted)]">({s.username})</span>
                    )}
                  </div>
                  <div className="flex items-center gap-3 text-xs text-[var(--muted)] shrink-0">
                    <span className="font-mono">{s.channelId}</span>
                  </div>
                  {s.message && (
                    <div className="hidden lg:block text-xs text-[var(--muted)] max-w-[200px] truncate" title={s.message}>
                      {s.message}
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </ConfigPanel>

        {/* RSS Feeds Panel */}
        <ConfigPanel title="RSS Feeds">
          {config.feeds.length === 0 ? (
            <p className="text-[var(--muted)] text-sm">
              No RSS feeds. Use <code className="px-1.5 py-0.5 bg-[var(--background)] rounded text-xs">.feed</code> commands to add them.
            </p>
          ) : (
            <div className="space-y-2">
              {config.feeds.map((f) => (
                <div
                  key={f.id}
                  className="flex items-center gap-3 px-3 py-2.5 bg-[var(--background)] rounded-lg border border-[var(--border)]"
                >
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-mono truncate" title={f.url}>{f.url}</p>
                    {f.message && (
                      <p className="text-xs text-[var(--muted)] mt-0.5 truncate" title={f.message}>
                        {f.message}
                      </p>
                    )}
                  </div>
                  <span className="text-xs text-[var(--muted)] font-mono shrink-0">{f.channelId}</span>
                </div>
              ))}
            </div>
          )}
        </ConfigPanel>
      </div>
    </div>
  );
}
