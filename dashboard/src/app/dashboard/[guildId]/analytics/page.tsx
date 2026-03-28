"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";

interface CommandStat {
  commandName: string;
  totalUsage: number;
}

interface DailyStat {
  date: string;
  totalUsage: number;
}

export default function AnalyticsPage() {
  const { guildId } = useParams();
  const [topCommands, setTopCommands] = useState<CommandStat[]>([]);
  const [dailyStats, setDailyStats] = useState<DailyStat[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!guildId) return;

    Promise.all([
      apiFetch<CommandStat[]>(`/api/guilds/${guildId}/analytics/commands`),
      apiFetch<DailyStat[]>(`/api/guilds/${guildId}/analytics/commands/daily?days=7`),
    ])
      .then(([commands, daily]) => {
        setTopCommands(commands);
        setDailyStats(daily);
      })
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, [guildId]);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="w-8 h-8 border-2 border-[var(--accent)] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-500/10 border border-red-500/30 rounded-xl p-6 text-center">
        <p className="text-red-400 font-medium">Failed to load analytics</p>
        <p className="text-red-400/70 text-sm mt-1">{error}</p>
      </div>
    );
  }

  const maxUsage = topCommands.length > 0 ? topCommands[0].totalUsage : 1;
  const maxDaily = dailyStats.length > 0 ? Math.max(...dailyStats.map((d) => d.totalUsage), 1) : 1;

  return (
    <div>
      <h1 className="text-2xl font-bold mb-2">Command Analytics</h1>
      <p className="text-[var(--muted)] mb-6">
        See which commands your server uses the most.
      </p>

      <div className="space-y-6">
        {/* Top Commands Bar Chart */}
        <div className="bg-[var(--card)] rounded-xl border border-[var(--border)] overflow-hidden">
          <div className="p-6 border-b border-[var(--border)]">
            <h2 className="text-xl font-semibold">Top Commands</h2>
            <p className="text-[var(--muted)] text-sm mt-1">Most used commands across all time</p>
          </div>
          <div className="p-6">
            {topCommands.length === 0 ? (
              <div className="text-center py-8">
                <p className="text-[var(--muted)] text-lg">No command usage data yet.</p>
                <p className="text-[var(--muted)] text-sm mt-2">
                  Command usage will be tracked as members use bot commands.
                </p>
              </div>
            ) : (
              <div className="space-y-3">
                {topCommands.map((cmd) => (
                  <div key={cmd.commandName} className="flex items-center gap-4">
                    <span className="text-sm font-mono w-32 truncate shrink-0" title={cmd.commandName}>
                      {cmd.commandName}
                    </span>
                    <div className="flex-1 h-7 bg-[var(--background)] rounded-lg overflow-hidden border border-[var(--border)]">
                      <div
                        className="h-full bg-[var(--accent)] rounded-lg transition-all duration-500"
                        style={{ width: `${(cmd.totalUsage / maxUsage) * 100}%` }}
                      />
                    </div>
                    <span className="text-sm text-[var(--muted)] w-16 text-right shrink-0">
                      {cmd.totalUsage.toLocaleString()}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Daily Usage Chart */}
        <div className="bg-[var(--card)] rounded-xl border border-[var(--border)] overflow-hidden">
          <div className="p-6 border-b border-[var(--border)]">
            <h2 className="text-xl font-semibold">Daily Usage</h2>
            <p className="text-[var(--muted)] text-sm mt-1">Total commands used per day (last 7 days)</p>
          </div>
          <div className="p-6">
            {dailyStats.length === 0 ? (
              <div className="text-center py-8">
                <p className="text-[var(--muted)]">No daily data available yet.</p>
              </div>
            ) : (
              <div className="flex items-end gap-2 h-48">
                {dailyStats.map((day) => {
                  const height = (day.totalUsage / maxDaily) * 100;
                  const dateLabel = new Date(day.date).toLocaleDateString(undefined, {
                    month: "short",
                    day: "numeric",
                  });
                  return (
                    <div key={day.date} className="flex-1 flex flex-col items-center gap-1">
                      <span className="text-xs text-[var(--muted)]">{day.totalUsage}</span>
                      <div className="w-full flex justify-center" style={{ height: "160px" }}>
                        <div
                          className="w-full max-w-12 bg-[var(--accent)] rounded-t-md self-end transition-all duration-500"
                          style={{ height: `${Math.max(height, 4)}%` }}
                        />
                      </div>
                      <span className="text-xs text-[var(--muted)]">{dateLabel}</span>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
