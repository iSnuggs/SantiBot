"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface AntiRaidConfig {
  configured: boolean;
  enabled: boolean;
  verifyChannelId: string | null;
  verifiedRoleId: string | null;
  verifyMessage: string | null;
  autoLockdownEnabled: boolean;
  lockdownJoinThreshold: number;
  lockdownTimeWindowSeconds: number;
  isLockedDown: boolean;
}

export default function AntiRaidPage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [config, setConfig] = useState<AntiRaidConfig | null>(null);

  const fetchData = () => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<AntiRaidConfig>(`/api/guilds/${guildId}/config/antiraid`)
      .then((data) => setConfig(data))
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
        <p className="text-red-400 mb-2">Failed to load anti-raid config</p>
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

  if (!config?.configured) {
    return (
      <div>
        <h1 className="text-2xl font-bold mb-6">Anti-Raid</h1>
        <p className="text-[var(--muted)] mb-6">
          Protect your server from raids with verification gates and auto-lockdown.
        </p>
        <ConfigPanel title="Anti-Raid Protection">
          <p className="text-[var(--muted)] text-sm py-4">
            Anti-raid not set up. Use <code className="px-1.5 py-0.5 bg-[var(--background)] border border-[var(--border)] rounded text-xs">.verify enable</code> to configure.
          </p>
        </ConfigPanel>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Anti-Raid</h1>
      <p className="text-[var(--muted)] mb-6">
        Protect your server from raids with verification gates and auto-lockdown.
      </p>

      <div className="space-y-6">
        <ConfigPanel title="Verification Settings" description="Read-only overview of your anti-raid configuration.">
          <div className="space-y-4">
            <div className="flex items-center justify-between py-2">
              <span className="text-sm font-medium">Status</span>
              <span
                className={`px-3 py-1 text-sm rounded-full font-semibold ${
                  config.enabled
                    ? "bg-green-500/15 text-green-400"
                    : "bg-red-500/15 text-red-400"
                }`}
              >
                {config.enabled ? "Enabled" : "Disabled"}
              </span>
            </div>

            <div className="py-2 border-t border-[var(--border)]">
              <span className="text-sm font-medium block mb-1">Verify Channel ID</span>
              <span className="text-sm text-[var(--muted)]">
                {config.verifyChannelId || "Not set"}
              </span>
            </div>

            <div className="py-2 border-t border-[var(--border)]">
              <span className="text-sm font-medium block mb-1">Verified Role ID</span>
              <span className="text-sm text-[var(--muted)]">
                {config.verifiedRoleId || "Not set"}
              </span>
            </div>

            <div className="py-2 border-t border-[var(--border)]">
              <span className="text-sm font-medium block mb-1">Verify Message</span>
              <span className="text-sm text-[var(--muted)]">
                {config.verifyMessage || "Default message"}
              </span>
            </div>
          </div>
        </ConfigPanel>

        <ConfigPanel title="Auto-Lockdown" description="Automatic server lockdown when join rate exceeds threshold.">
          <div className="space-y-4">
            <div className="flex items-center justify-between py-2">
              <span className="text-sm font-medium">Auto-Lockdown</span>
              <span
                className={`px-3 py-1 text-sm rounded-full font-semibold ${
                  config.autoLockdownEnabled
                    ? "bg-green-500/15 text-green-400"
                    : "bg-red-500/15 text-red-400"
                }`}
              >
                {config.autoLockdownEnabled ? "Enabled" : "Disabled"}
              </span>
            </div>

            <div className="py-2 border-t border-[var(--border)]">
              <span className="text-sm font-medium block mb-1">Join Threshold</span>
              <span className="text-sm text-[var(--muted)]">
                {config.lockdownJoinThreshold} joins
              </span>
            </div>

            <div className="py-2 border-t border-[var(--border)]">
              <span className="text-sm font-medium block mb-1">Time Window</span>
              <span className="text-sm text-[var(--muted)]">
                {config.lockdownTimeWindowSeconds} seconds
              </span>
            </div>

            <div className="py-2 border-t border-[var(--border)]">
              <span className="text-sm font-medium block mb-1">Current Status</span>
              <span
                className={`px-3 py-1 text-sm rounded-full font-bold ${
                  config.isLockedDown
                    ? "bg-red-500/15 text-red-400"
                    : "bg-green-500/15 text-green-400"
                }`}
              >
                {config.isLockedDown ? "LOCKED DOWN" : "Normal"}
              </span>
            </div>
          </div>
        </ConfigPanel>
      </div>
    </div>
  );
}
