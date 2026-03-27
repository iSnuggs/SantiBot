"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface ModSettings {
  modLogChannelId: string | null;
  dmOnAction: boolean;
  deleteModCommands: boolean;
  protectedRoleIds: string[];
}

interface AutoPunishRule {
  id: number;
  caseCount: number;
  timeWindowHours: number;
  action: string;
  actionDurationMinutes: number | null;
}

interface ModCase {
  caseNumber: number;
  caseType: string;
  targetUserId: string;
  moderatorUserId: string;
  reason: string | null;
  createdAt: string;
}

interface ModCasesConfig {
  settings: ModSettings;
  autopunish: AutoPunishRule[];
  cases: ModCase[];
}

const CASE_TYPE_COLORS: Record<string, string> = {
  Warn: "bg-yellow-500/15 text-yellow-400",
  Mute: "bg-orange-500/15 text-orange-400",
  Kick: "bg-red-500/15 text-red-400",
  Ban: "bg-red-800/20 text-red-300",
  Unmute: "bg-green-500/15 text-green-400",
  Unban: "bg-green-500/15 text-green-400",
  Note: "bg-blue-500/15 text-blue-400",
};

function getCaseColor(caseType: string): string {
  return CASE_TYPE_COLORS[caseType] || "bg-[var(--muted)]/15 text-[var(--muted)]";
}

export default function ModCasesPage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [data, setData] = useState<ModCasesConfig | null>(null);

  const fetchData = () => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<ModCasesConfig>(`/api/guilds/${guildId}/config/modcases`)
      .then((d) => setData(d))
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
        <p className="text-red-400 mb-2">Failed to load moderation data</p>
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

  if (!data) return null;

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Mod Cases</h1>
      <p className="text-[var(--muted)] mb-6">
        View moderation settings, auto-punish rules, and recent cases for this server.
      </p>

      <div className="space-y-6">
        {/* Panel 1: Mod Settings */}
        <ConfigPanel title="Mod Settings">
          <div className="space-y-4">
            <div className="flex items-center justify-between py-2">
              <span className="text-sm font-medium">Mod Log Channel ID</span>
              <span className="text-sm text-[var(--muted)]">
                {data.settings.modLogChannelId || "Not set"}
              </span>
            </div>
            <div className="flex items-center justify-between py-2 border-t border-[var(--border)]">
              <span className="text-sm font-medium">DM on Action</span>
              <span
                className={`px-2.5 py-0.5 text-xs font-medium rounded-full ${
                  data.settings.dmOnAction
                    ? "bg-green-500/15 text-green-400"
                    : "bg-red-500/15 text-red-400"
                }`}
              >
                {data.settings.dmOnAction ? "Yes" : "No"}
              </span>
            </div>
            <div className="flex items-center justify-between py-2 border-t border-[var(--border)]">
              <span className="text-sm font-medium">Delete Mod Commands</span>
              <span
                className={`px-2.5 py-0.5 text-xs font-medium rounded-full ${
                  data.settings.deleteModCommands
                    ? "bg-green-500/15 text-green-400"
                    : "bg-red-500/15 text-red-400"
                }`}
              >
                {data.settings.deleteModCommands ? "Yes" : "No"}
              </span>
            </div>
          </div>
        </ConfigPanel>

        {/* Panel 2: Auto-Punish Rules */}
        <ConfigPanel title="Auto-Punish Rules">
          {data.autopunish.length === 0 ? (
            <p className="text-[var(--muted)] text-sm">No auto-punish rules configured.</p>
          ) : (
            <div className="space-y-3">
              {data.autopunish.map((rule) => (
                <div
                  key={rule.id}
                  className="flex items-center justify-between p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]"
                >
                  <span className="text-sm">
                    <span className="font-medium">{rule.caseCount}</span> cases in{" "}
                    <span className="font-medium">{rule.timeWindowHours}h</span>
                  </span>
                  <span className="text-sm text-[var(--accent)] font-medium">
                    {rule.action}
                    {rule.actionDurationMinutes
                      ? ` (${rule.actionDurationMinutes}min)`
                      : ""}
                  </span>
                </div>
              ))}
            </div>
          )}
        </ConfigPanel>

        {/* Panel 3: Recent Cases */}
        <ConfigPanel title="Recent Cases">
          {data.cases.length === 0 ? (
            <p className="text-[var(--muted)] text-sm">No moderation cases recorded.</p>
          ) : (
            <div className="space-y-2">
              {data.cases.map((c) => (
                <div
                  key={c.caseNumber}
                  className="flex items-start gap-3 p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]"
                >
                  {/* Case number */}
                  <span className="text-xs text-[var(--muted)] font-mono mt-0.5">
                    #{c.caseNumber}
                  </span>

                  {/* Type badge */}
                  <span
                    className={`px-2 py-0.5 text-xs font-medium rounded-full whitespace-nowrap ${getCaseColor(
                      c.caseType
                    )}`}
                  >
                    {c.caseType}
                  </span>

                  {/* Details */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 text-xs text-[var(--muted)]">
                      <span>Target: {c.targetUserId}</span>
                      <span>|</span>
                      <span>Mod: {c.moderatorUserId}</span>
                    </div>
                    {c.reason && (
                      <p className="text-sm mt-1 truncate">{c.reason}</p>
                    )}
                  </div>

                  {/* Date */}
                  <span className="text-xs text-[var(--muted)] whitespace-nowrap">
                    {new Date(c.createdAt).toLocaleDateString()}
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
