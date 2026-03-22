"use client";

import Link from "next/link";
import { useAuth } from "@/hooks/useAuth";

export default function DashboardHome() {
  const { user, loading, logout } = useAuth();

  if (loading) {
    return (
      <main className="flex min-h-screen items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-[var(--accent)]" />
      </main>
    );
  }

  if (!user) {
    return (
      <main className="flex min-h-screen items-center justify-center">
        <div className="text-center">
          <p className="text-[var(--muted)] mb-4">You need to log in first.</p>
          <Link href="/" className="text-[var(--accent)] hover:underline">
            Go to login
          </Link>
        </div>
      </main>
    );
  }

  // Placeholder guild list — in production, fetched from /api/guilds
  const guilds = [
    {
      id: "placeholder",
      name: "Select a server to manage",
      iconUrl: null,
    },
  ];

  return (
    <main className="min-h-screen p-8">
      <div className="max-w-4xl mx-auto">
        <div className="flex items-center justify-between mb-8">
          <div>
            <h1 className="text-3xl font-bold">
              Welcome, <span className="text-[var(--accent)]">{user.username}</span>
            </h1>
            <p className="text-[var(--muted)] mt-1">Select a server to manage</p>
          </div>
          <button
            onClick={logout}
            className="px-4 py-2 text-sm border border-[var(--border)] rounded-lg hover:bg-[var(--card)] transition-colors"
          >
            Logout
          </button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {guilds.map((guild) => (
            <Link
              key={guild.id}
              href={`/dashboard/${guild.id}/overview`}
              className="bg-[var(--card)] border border-[var(--border)] rounded-xl p-6 hover:border-[var(--accent)] transition-colors group"
            >
              <div className="flex items-center gap-4">
                <div className="w-12 h-12 rounded-full bg-[var(--accent)] flex items-center justify-center text-white font-bold text-lg">
                  {guild.name.charAt(0)}
                </div>
                <div>
                  <h3 className="font-semibold group-hover:text-[var(--accent)] transition-colors">
                    {guild.name}
                  </h3>
                </div>
              </div>
            </Link>
          ))}
        </div>

        <div className="mt-8 p-6 bg-[var(--card)] border border-[var(--border)] rounded-xl">
          <h2 className="font-semibold mb-2">Getting Started</h2>
          <p className="text-[var(--muted)] text-sm">
            Once the bot is running and connected to your Discord server, your servers will appear here.
            Configure the Dashboard API with your Discord application credentials in{" "}
            <code className="text-[var(--accent)]">appsettings.json</code>.
          </p>
        </div>
      </div>
    </main>
  );
}
