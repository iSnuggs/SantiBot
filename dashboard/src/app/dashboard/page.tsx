"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/hooks/useAuth";
import { apiFetch } from "@/lib/api";
import { Guild } from "@/lib/types";

export default function DashboardHome() {
  const { user, loading: authLoading, logout } = useAuth();
  const [guilds, setGuilds] = useState<Guild[]>([]);
  const [guildsLoading, setGuildsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Fetch the user's servers once they're logged in
  useEffect(() => {
    if (!user) return;

    const fetchGuilds = async () => {
      try {
        setGuildsLoading(true);
        setError(null);
        const data = await apiFetch<Guild[]>("/api/guilds");
        setGuilds(data);
      } catch (err: any) {
        // If their Discord token expired, tell them to re-login
        if (err.message?.includes("discord_token_expired")) {
          setError("Your Discord session expired. Please log in again.");
        } else {
          setError("Failed to load your servers. Please try again.");
        }
      } finally {
        setGuildsLoading(false);
      }
    };

    fetchGuilds();
  }, [user]);

  if (authLoading) {
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

        {/* Error state — shows a message with a retry button */}
        {error && (
          <div className="mb-6 p-4 bg-red-500/10 border border-red-500/30 rounded-xl text-red-400">
            <p>{error}</p>
            {error.includes("expired") ? (
              <Link href="/" className="text-[var(--accent)] hover:underline text-sm mt-2 inline-block">
                Log in again
              </Link>
            ) : (
              <button
                onClick={() => window.location.reload()}
                className="text-[var(--accent)] hover:underline text-sm mt-2"
              >
                Retry
              </button>
            )}
          </div>
        )}

        {/* Loading state — shows spinner while fetching servers */}
        {guildsLoading && (
          <div className="flex items-center justify-center py-16">
            <div className="text-center">
              <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-[var(--accent)] mx-auto mb-4" />
              <p className="text-[var(--muted)] text-sm">Loading your servers...</p>
            </div>
          </div>
        )}

        {/* Server grid — each card links to that server's dashboard */}
        {!guildsLoading && !error && guilds.length > 0 && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {guilds.map((guild) => (
              <Link
                key={guild.id}
                href={`/dashboard/${guild.id}/overview`}
                className="bg-[var(--card)] border border-[var(--border)] rounded-xl p-6 hover:border-[var(--accent)] transition-colors group"
              >
                <div className="flex items-center gap-4">
                  {guild.iconUrl ? (
                    <img
                      src={guild.iconUrl}
                      alt={guild.name}
                      className="w-12 h-12 rounded-full"
                    />
                  ) : (
                    <div className="w-12 h-12 rounded-full bg-[var(--accent)] flex items-center justify-center text-white font-bold text-lg">
                      {guild.name.charAt(0)}
                    </div>
                  )}
                  <div>
                    <h3 className="font-semibold group-hover:text-[var(--accent)] transition-colors">
                      {guild.name}
                    </h3>
                    {guild.owner && (
                      <span className="text-xs text-[var(--muted)]">Owner</span>
                    )}
                  </div>
                </div>
              </Link>
            ))}
          </div>
        )}

        {/* Empty state — user has no servers they can manage */}
        {!guildsLoading && !error && guilds.length === 0 && (
          <div className="text-center py-16">
            <p className="text-[var(--muted)] text-lg mb-2">No manageable servers found</p>
            <p className="text-[var(--muted)] text-sm">
              You need the &quot;Manage Server&quot; permission in a Discord server to configure it here.
            </p>
          </div>
        )}

        <div className="mt-8 p-6 bg-[var(--card)] border border-[var(--border)] rounded-xl">
          <h2 className="font-semibold mb-2">Getting Started</h2>
          <p className="text-[var(--muted)] text-sm">
            Servers where you have the &quot;Manage Server&quot; permission are shown above.
            Click a server to configure SantiBot&apos;s settings, moderation, automod, and more.
          </p>
        </div>
      </div>
    </main>
  );
}
