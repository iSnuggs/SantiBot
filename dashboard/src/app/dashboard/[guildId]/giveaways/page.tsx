"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface Giveaway {
  id: string;
  message: string;
  channelId: string;
  endsAt: string;
  winnerCount: number;
  requiredRoleId: string | null;
}

export default function GiveawaysPage() {
  const { guildId } = useParams();
  const [giveaways, setGiveaways] = useState<Giveaway[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!guildId) return;
    setLoading(true);
    apiFetch<Giveaway[]>(`/api/guilds/${guildId}/config/giveaways`)
      .then((data) => {
        setGiveaways(data);
        setError(null);
      })
      .catch((err) => {
        setError(err.message || "Failed to load giveaways");
      })
      .finally(() => setLoading(false));
  }, [guildId]);

  const isActive = (endsAt: string) => new Date(endsAt) > new Date();

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Giveaways</h1>
      <p className="text-[var(--muted)] mb-6">
        Manage server giveaways. Create, track, and configure giveaway settings for your community.
      </p>

      <div className="space-y-6">
        <ConfigPanel title="Giveaway List">
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <div className="w-6 h-6 border-2 border-[var(--accent)] border-t-transparent rounded-full animate-spin" />
              <span className="ml-3 text-sm text-[var(--muted)]">Loading giveaways...</span>
            </div>
          ) : error ? (
            <div className="text-center py-8">
              <p className="text-red-400 text-sm">{error}</p>
              <button
                onClick={() => {
                  setLoading(true);
                  setError(null);
                  apiFetch<Giveaway[]>(`/api/guilds/${guildId}/config/giveaways`)
                    .then((data) => setGiveaways(data))
                    .catch((err) => setError(err.message || "Failed to load giveaways"))
                    .finally(() => setLoading(false));
                }}
                className="mt-3 px-4 py-2 text-sm rounded-lg bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white transition-colors"
              >
                Retry
              </button>
            </div>
          ) : giveaways.length === 0 ? (
            <p className="text-[var(--muted)] text-sm">
              No giveaways found. Create them with bot commands.
            </p>
          ) : (
            <div className="space-y-3">
              {giveaways.map((g) => (
                <div
                  key={g.id}
                  className="flex items-center justify-between p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]"
                >
                  <div>
                    <p className="text-sm font-medium">{g.message}</p>
                    <p className="text-xs text-[var(--muted)]">
                      {g.winnerCount} winner{g.winnerCount !== 1 ? "s" : ""} &middot; Ends{" "}
                      {new Date(g.endsAt).toLocaleDateString()} &middot; Channel: {g.channelId}
                      {g.requiredRoleId && (
                        <> &middot; Requires role: {g.requiredRoleId}</>
                      )}
                    </p>
                  </div>
                  {isActive(g.endsAt) ? (
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
