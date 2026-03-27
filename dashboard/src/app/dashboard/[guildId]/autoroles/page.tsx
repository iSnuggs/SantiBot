"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface SettingsConfig {
  autoAssignRoleIds: string;
}

export default function AutorolesPage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [roleIds, setRoleIds] = useState<string[]>([]);

  const fetchData = () => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<SettingsConfig>(`/api/guilds/${guildId}/config/settings`)
      .then((data) => {
        const ids = data.autoAssignRoleIds
          ? data.autoAssignRoleIds.split(",").map((id) => id.trim()).filter(Boolean)
          : [];
        setRoleIds(ids);
      })
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
        <p className="text-red-400 mb-2">Failed to load autorole settings</p>
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
      <h1 className="text-2xl font-bold mb-6">Autoroles</h1>
      <p className="text-[var(--muted)] mb-6">
        Roles that are automatically assigned to new members when they join the server.
      </p>

      <ConfigPanel title="Auto-Assigned Roles">
        {roleIds.length === 0 ? (
          <p className="text-[var(--muted)] text-sm">
            No autoroles configured. Use <code className="px-1.5 py-0.5 bg-[var(--background)] rounded text-xs">.aar</code> commands to set them up.
          </p>
        ) : (
          <div className="space-y-2">
            {roleIds.map((id, idx) => (
              <div
                key={idx}
                className="flex items-center gap-3 px-3 py-2.5 bg-[var(--background)] rounded-lg border border-[var(--border)]"
              >
                <div className="w-2 h-2 rounded-full bg-[var(--accent)]" />
                <span className="text-sm font-mono">{id}</span>
              </div>
            ))}
            <p className="text-xs text-[var(--muted)] mt-3">
              {roleIds.length} role{roleIds.length !== 1 ? "s" : ""} will be assigned to new members on join.
            </p>
          </div>
        )}
      </ConfigPanel>
    </div>
  );
}
