"use client";

import { useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";

interface UserResult {
  userId: string;
  username: string;
  xp: number;
  level: number;
  warnings: number;
}

export default function UserSearchPage() {
  const { guildId } = useParams();
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<UserResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searched, setSearched] = useState(false);

  const doSearch = async () => {
    if (!guildId || query.trim().length < 2) return;

    setLoading(true);
    setError(null);
    setSearched(true);

    try {
      const res = await apiFetch<UserResult[]>(
        `/api/guilds/${guildId}/users/search?q=${encodeURIComponent(query.trim())}`
      );
      setResults(res);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") doSearch();
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-2">User Search</h1>
      <p className="text-[var(--muted)] mb-6">
        Look up any member by username or user ID to see their XP, level, and warnings.
      </p>

      {/* Search Bar */}
      <div className="flex gap-3 mb-6">
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Enter a username or user ID..."
          className="flex-1 px-4 py-2.5 bg-[var(--card)] border border-[var(--border)] rounded-lg text-sm focus:border-[var(--accent)] focus:outline-none transition-colors"
        />
        <button
          onClick={doSearch}
          disabled={loading || query.trim().length < 2}
          className="px-6 py-2.5 text-sm rounded-lg bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white disabled:opacity-50 transition-colors font-medium"
        >
          {loading ? "Searching..." : "Search"}
        </button>
      </div>

      {/* Error */}
      {error && (
        <div className="bg-red-500/10 border border-red-500/30 rounded-xl p-4 mb-6 text-center">
          <p className="text-red-400 text-sm">{error}</p>
        </div>
      )}

      {/* Results */}
      {!searched && !loading && (
        <div className="bg-[var(--card)] rounded-xl border border-[var(--border)] p-12 text-center">
          <p className="text-[var(--muted)] text-lg">Search for a user by name or ID.</p>
          <p className="text-[var(--muted)] text-sm mt-2">
            Results will show their XP, level, and moderation history in this server.
          </p>
        </div>
      )}

      {searched && !loading && results.length === 0 && !error && (
        <div className="bg-[var(--card)] rounded-xl border border-[var(--border)] p-12 text-center">
          <p className="text-[var(--muted)] text-lg">No users found.</p>
          <p className="text-[var(--muted)] text-sm mt-2">
            Try a different username or paste a user ID directly.
          </p>
        </div>
      )}

      {results.length > 0 && (
        <div className="bg-[var(--card)] rounded-xl border border-[var(--border)] overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-[var(--border)] bg-[var(--card-hover)]">
                  <th className="text-left px-4 py-3 font-medium text-[var(--muted)]">User</th>
                  <th className="text-left px-4 py-3 font-medium text-[var(--muted)]">User ID</th>
                  <th className="text-right px-4 py-3 font-medium text-[var(--muted)]">XP</th>
                  <th className="text-right px-4 py-3 font-medium text-[var(--muted)]">Level</th>
                  <th className="text-right px-4 py-3 font-medium text-[var(--muted)]">Warnings</th>
                </tr>
              </thead>
              <tbody>
                {results.map((user) => (
                  <tr
                    key={user.userId}
                    className="border-b border-[var(--border)] last:border-b-0 hover:bg-[var(--card-hover)] transition-colors"
                  >
                    <td className="px-4 py-3 font-medium">{user.username}</td>
                    <td className="px-4 py-3 text-[var(--muted)] font-mono text-xs">
                      {user.userId}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <span className="text-[var(--accent)] font-medium">
                        {user.xp.toLocaleString()}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right font-medium">{user.level}</td>
                    <td className="px-4 py-3 text-right">
                      <span
                        className={
                          user.warnings > 0
                            ? "text-[var(--error)] font-medium"
                            : "text-[var(--muted)]"
                        }
                      >
                        {user.warnings}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
