"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface ReactionRole {
  id: number;
  channelId: string;
  messageId: string;
  emote: string;
  roleId: string;
  group: number;
  levelReq: number;
}

export default function ReactionRolesPage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [roles, setRoles] = useState<ReactionRole[]>([]);

  const fetchData = () => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<ReactionRole[]>(`/api/guilds/${guildId}/config/reactionroles`)
      .then((data) => setRoles(data))
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
        <p className="text-red-400 mb-2">Failed to load reaction roles</p>
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

  // Group roles by messageId
  const grouped = roles.reduce<Record<string, ReactionRole[]>>((acc, role) => {
    if (!acc[role.messageId]) acc[role.messageId] = [];
    acc[role.messageId].push(role);
    return acc;
  }, {});

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Reaction Roles</h1>
      <p className="text-[var(--muted)] mb-6">
        View the reaction role mappings configured for this server.
        Members react to messages to receive roles automatically.
      </p>

      {roles.length === 0 ? (
        <ConfigPanel title="Reaction Roles">
          <p className="text-[var(--muted)] text-sm">
            No reaction roles configured. Use <code className="px-1.5 py-0.5 bg-[var(--background)] rounded text-xs">.rero</code> commands to set them up.
          </p>
        </ConfigPanel>
      ) : (
        <div className="space-y-4">
          {Object.entries(grouped).map(([messageId, mappings]) => (
            <ConfigPanel key={messageId} title={`Message: ${messageId}`}>
              <div className="space-y-3">
                <div className="flex items-center gap-2 text-sm">
                  <span className="text-[var(--muted)]">Channel ID:</span>
                  <span className="px-2 py-0.5 bg-[var(--background)] border border-[var(--border)] rounded text-xs font-mono">
                    {mappings[0].channelId}
                  </span>
                </div>

                <div className="flex items-center gap-4 text-sm">
                  <div className="flex items-center gap-2">
                    <span className="text-[var(--muted)]">Group:</span>
                    <span>{mappings[0].group}</span>
                  </div>
                  {mappings[0].levelReq > 0 && (
                    <div className="flex items-center gap-2">
                      <span className="text-[var(--muted)]">Level Requirement:</span>
                      <span className="px-2 py-0.5 bg-[var(--accent)]/10 border border-[var(--accent)]/30 rounded text-xs text-[var(--accent)]">
                        Lv. {mappings[0].levelReq}
                      </span>
                    </div>
                  )}
                </div>

                <div className="border-t border-[var(--border)] pt-3">
                  <p className="text-xs text-[var(--muted)] mb-2 uppercase tracking-wider font-medium">Emote &rarr; Role Mappings</p>
                  <div className="space-y-1.5">
                    {mappings.map((m) => (
                      <div
                        key={m.id}
                        className="flex items-center gap-3 px-3 py-2 bg-[var(--background)] rounded-lg border border-[var(--border)] text-sm"
                      >
                        <span className="text-lg">{m.emote}</span>
                        <span className="text-[var(--muted)]">&rarr;</span>
                        <span className="font-mono text-xs">{m.roleId}</span>
                        {m.levelReq > 0 && (
                          <span className="ml-auto text-xs text-[var(--muted)]">
                            Lv. {m.levelReq}
                          </span>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </ConfigPanel>
          ))}
        </div>
      )}
    </div>
  );
}
