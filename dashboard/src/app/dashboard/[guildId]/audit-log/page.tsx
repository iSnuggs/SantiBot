"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";

interface AuditEntry {
  id: number;
  userId: string;
  username: string;
  action: string;
  section: string;
  timestamp: string;
}

export default function AuditLogPage() {
  const { guildId } = useParams();
  const [entries, setEntries] = useState<AuditEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!guildId) return;

    apiFetch<AuditEntry[]>(`/api/guilds/${guildId}/audit`)
      .then((res) => setEntries(res))
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
        <p className="text-red-400 font-medium">Failed to load audit log</p>
        <p className="text-red-400/70 text-sm mt-1">{error}</p>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold mb-2">Audit Log</h1>
      <p className="text-[var(--muted)] mb-6">
        Track who changed what in the dashboard. All configuration changes are logged here.
      </p>

      {entries.length === 0 ? (
        <div className="bg-[var(--card)] rounded-xl border border-[var(--border)] p-12 text-center">
          <p className="text-[var(--muted)] text-lg">No audit log entries yet.</p>
          <p className="text-[var(--muted)] text-sm mt-2">
            Changes made through the dashboard will appear here.
          </p>
        </div>
      ) : (
        <div className="bg-[var(--card)] rounded-xl border border-[var(--border)] overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-[var(--border)] bg-[var(--card-hover)]">
                  <th className="text-left px-4 py-3 font-medium text-[var(--muted)]">Timestamp</th>
                  <th className="text-left px-4 py-3 font-medium text-[var(--muted)]">User</th>
                  <th className="text-left px-4 py-3 font-medium text-[var(--muted)]">Action</th>
                  <th className="text-left px-4 py-3 font-medium text-[var(--muted)]">Section</th>
                </tr>
              </thead>
              <tbody>
                {entries.map((entry) => (
                  <tr
                    key={entry.id}
                    className="border-b border-[var(--border)] last:border-b-0 hover:bg-[var(--card-hover)] transition-colors"
                  >
                    <td className="px-4 py-3 text-[var(--muted)] whitespace-nowrap">
                      {new Date(entry.timestamp).toLocaleString()}
                    </td>
                    <td className="px-4 py-3 font-medium">{entry.username}</td>
                    <td className="px-4 py-3">{entry.action}</td>
                    <td className="px-4 py-3">
                      <span className="inline-block px-2 py-0.5 rounded-md bg-[var(--accent)]/10 text-[var(--accent)] text-xs font-medium">
                        {entry.section}
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
