"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel, { Toggle } from "@/components/ConfigPanel";

interface PermissionOverride {
  index: number;
  primaryTarget: string;
  primaryTargetId: string;
  secondaryTarget: string;
  secondaryTargetName: string;
  state: boolean;
}

interface PermissionsConfig {
  verbosePermissions: boolean;
  permissionRole: string;
  overrides: PermissionOverride[];
}

export default function PermissionsPage() {
  const { guildId } = useParams();
  const [data, setData] = useState<PermissionsConfig | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!guildId) return;

    apiFetch<PermissionsConfig>(`/api/guilds/${guildId}/config/permissions`)
      .then((res) => setData(res))
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
        <p className="text-red-400 font-medium">Failed to load permissions</p>
        <p className="text-red-400/70 text-sm mt-1">{error}</p>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Permissions</h1>
      <p className="text-[var(--muted)] mb-6">
        View command permission overrides for roles and users. Permissions are configured via bot commands.
      </p>

      <div className="space-y-6">
        <ConfigPanel title="Global Permission Settings">
          <Toggle
            label="Verbose Permissions (use .verbose command to change)"
            checked={data?.verbosePermissions ?? false}
            onChange={() => {}}
          />
          <div className="mb-4">
            <label className="block text-sm font-medium mb-1.5">Permission Role</label>
            <div className="w-full px-3 py-2 bg-[var(--background)] border border-[var(--border)] rounded-lg text-sm">
              {data?.permissionRole || "Not set"}
            </div>
          </div>
        </ConfigPanel>

        <ConfigPanel title="Permission Overrides">
          {!data?.overrides || data.overrides.length === 0 ? (
            <p className="text-[var(--muted)] text-sm">No permission overrides configured.</p>
          ) : (
            <div className="space-y-3">
              {data.overrides.map((o) => (
                <div
                  key={o.index}
                  className="flex items-center justify-between p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]"
                >
                  <div>
                    <p className="text-sm font-medium">
                      <code className="text-[var(--accent)]">{o.secondaryTargetName}</code>
                    </p>
                    <p className="text-xs text-[var(--muted)]">
                      {o.primaryTarget} ({o.primaryTargetId})
                    </p>
                  </div>
                  <span
                    className={`text-xs px-2 py-1 rounded-full ${
                      o.state
                        ? "bg-green-500/20 text-green-400"
                        : "bg-red-500/20 text-red-400"
                    }`}
                  >
                    {o.state ? "Allow" : "Deny"}
                  </span>
                </div>
              ))}
            </div>
          )}
        </ConfigPanel>
      </div>
    </div>
  );
}
