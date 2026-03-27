"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";

interface OverviewData {
  activeFeatures: number;
  giveaways: number;
  activePolls: number;
  activeForms: number;
  expressions: number;
  autoPurgeChannels: number;
  starboardEnabled: boolean;
}

export default function OverviewPage() {
  const { guildId } = useParams();
  const [data, setData] = useState<OverviewData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!guildId) return;

    setLoading(true);
    apiFetch<OverviewData>(`/api/guilds/${guildId}/config/overview`)
      .then((res) => {
        setData(res);
        setError(null);
      })
      .catch((err) => {
        setError(err.message || "Failed to load overview data");
      })
      .finally(() => setLoading(false));
  }, [guildId]);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="animate-spin rounded-full h-8 w-8 border-2 border-[var(--accent)] border-t-transparent" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-500/10 border border-red-500/30 rounded-xl p-6 text-center">
        <p className="text-red-400 font-medium">Failed to load overview</p>
        <p className="text-red-400/70 text-sm mt-1">{error}</p>
      </div>
    );
  }

  const stats = [
    { label: "Active Features", value: data?.activeFeatures ?? 0, icon: "⚡" },
    { label: "Expressions", value: data?.expressions ?? 0, icon: "💬" },
    { label: "Active Polls", value: data?.activePolls ?? 0, icon: "📊" },
    { label: "Active Giveaways", value: data?.giveaways ?? 0, icon: "🎉" },
  ];

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Server Overview</h1>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {stats.map((stat) => (
          <div
            key={stat.label}
            className="bg-[var(--card)] border border-[var(--border)] rounded-xl p-5"
          >
            <div className="flex items-center gap-3 mb-2">
              <span className="text-2xl">{stat.icon}</span>
              <span className="text-[var(--muted)] text-sm">{stat.label}</span>
            </div>
            <p className="text-2xl font-bold">{stat.value}</p>
          </div>
        ))}
      </div>

      <div className="bg-[var(--card)] border border-[var(--border)] rounded-xl p-6">
        <h2 className="font-semibold mb-4">Quick Actions</h2>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          {[
            { name: "Set Prefix", icon: "⌨️" },
            { name: "View Logs", icon: "📝" },
            { name: "Manage Roles", icon: "🎭" },
            { name: "Bot Settings", icon: "⚙️" },
          ].map((action) => (
            <button
              key={action.name}
              className="flex flex-col items-center gap-2 p-4 rounded-lg bg-[var(--background)] hover:bg-[var(--card-hover)] border border-[var(--border)] transition-colors"
            >
              <span className="text-2xl">{action.icon}</span>
              <span className="text-xs text-[var(--muted)]">{action.name}</span>
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}
