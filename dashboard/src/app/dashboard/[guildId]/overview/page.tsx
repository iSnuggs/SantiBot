"use client";

import { useParams } from "next/navigation";

export default function OverviewPage() {
  const { guildId } = useParams();

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Server Overview</h1>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {[
          { label: "Members", value: "--", icon: "👥" },
          { label: "Channels", value: "--", icon: "💬" },
          { label: "Active Features", value: "--", icon: "⚡" },
          { label: "Commands Used", value: "--", icon: "📈" },
        ].map((stat) => (
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
