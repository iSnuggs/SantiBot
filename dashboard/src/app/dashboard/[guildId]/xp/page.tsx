"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel, { Toggle, InputField, SelectField } from "@/components/ConfigPanel";

interface XPConfig {
  configured: boolean;
  guildId: string;
  trackedUsers: number;
}

export default function XPPage() {
  const { guildId } = useParams();
  const [data, setData] = useState<XPConfig | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Local-only settings (will be wired when XP backend is expanded)
  const [enabled, setEnabled] = useState(true);
  const [levelUpMessages, setLevelUpMessages] = useState(true);
  const [voiceXP, setVoiceXP] = useState(false);
  const [stackRoles, setStackRoles] = useState(true);
  const [xpRate, setXpRate] = useState("1x");
  const [minXP, setMinXP] = useState("15");
  const [maxXP, setMaxXP] = useState("25");
  const [cooldown, setCooldown] = useState("60");
  const [levelUpChannel, setLevelUpChannel] = useState("");

  useEffect(() => {
    if (!guildId) return;

    apiFetch<XPConfig>(`/api/guilds/${guildId}/config/xp`)
      .then((res) => {
        setData(res);
        setEnabled(res.configured);
      })
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
        <p className="text-red-400 font-medium">Failed to load XP settings</p>
        <p className="text-red-400/70 text-sm mt-1">{error}</p>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">XP &amp; Leveling</h1>
      <p className="text-[var(--muted)] mb-6">
        Configure the XP and leveling system. Reward active members with levels and role rewards.
      </p>

      <div className="space-y-6">
        {/* Live stat from API */}
        <ConfigPanel title="XP Status">
          <div className="flex items-center gap-6">
            <div className="bg-[var(--background)] rounded-lg p-4 flex-1 text-center border border-[var(--border)]">
              <p className="text-2xl font-bold text-[var(--accent)]">{data?.trackedUsers ?? 0}</p>
              <p className="text-xs text-[var(--muted)] mt-1">Tracked Users</p>
            </div>
            <div className="bg-[var(--background)] rounded-lg p-4 flex-1 text-center border border-[var(--border)]">
              <p className="text-2xl font-bold text-[var(--accent)]">{data?.configured ? "Active" : "Inactive"}</p>
              <p className="text-xs text-[var(--muted)] mt-1">System Status</p>
            </div>
          </div>
        </ConfigPanel>

        <ConfigPanel title="General Settings">
          <Toggle label="Enable XP System" checked={enabled} onChange={setEnabled} />
          <Toggle label="Level-Up Announcements" checked={levelUpMessages} onChange={setLevelUpMessages} />
          <Toggle label="Voice Channel XP" checked={voiceXP} onChange={setVoiceXP} />
          <Toggle label="Stack Level Roles" checked={stackRoles} onChange={setStackRoles} />
          <InputField
            label="Level-Up Channel ID"
            value={levelUpChannel}
            onChange={setLevelUpChannel}
            placeholder="Leave empty for current channel"
          />
        </ConfigPanel>

        <ConfigPanel title="XP Rates">
          <p className="text-sm text-[var(--muted)] mb-4 bg-[var(--background)] border border-[var(--border)] rounded-lg p-3">
            XP rates are configured via bot commands. The settings below are for reference only.
          </p>
          <SelectField
            label="XP Multiplier"
            value={xpRate}
            onChange={setXpRate}
            options={[
              { value: "0.5x", label: "0.5x (Slow)" },
              { value: "1x", label: "1x (Normal)" },
              { value: "1.5x", label: "1.5x (Fast)" },
              { value: "2x", label: "2x (Very Fast)" },
              { value: "3x", label: "3x (Ultra)" },
            ]}
          />
          <InputField
            label="Min XP Per Message"
            value={minXP}
            onChange={setMinXP}
            placeholder="15"
            type="number"
          />
          <InputField
            label="Max XP Per Message"
            value={maxXP}
            onChange={setMaxXP}
            placeholder="25"
            type="number"
          />
          <InputField
            label="Cooldown (seconds)"
            value={cooldown}
            onChange={setCooldown}
            placeholder="60"
            type="number"
          />
        </ConfigPanel>
      </div>
    </div>
  );
}
