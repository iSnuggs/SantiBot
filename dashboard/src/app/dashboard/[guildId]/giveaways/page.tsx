"use client";

import { useState } from "react";
import ConfigPanel, { Toggle, InputField, SelectField } from "@/components/ConfigPanel";

interface Giveaway {
  id: string;
  prize: string;
  winners: number;
  endsAt: string;
  channel: string;
  active: boolean;
}

export default function GiveawaysPage() {
  const [enabled, setEnabled] = useState(false);
  const [dmWinners, setDmWinners] = useState(true);
  const [requireRole, setRequireRole] = useState(false);
  const [defaultDuration, setDefaultDuration] = useState("24h");
  const [hasChanges, setHasChanges] = useState(false);

  const [giveaways] = useState<Giveaway[]>([
    { id: "1", prize: "Nitro Classic", winners: 1, endsAt: "2026-03-25", channel: "#giveaways", active: true },
    { id: "2", prize: "Steam Gift Card", winners: 2, endsAt: "2026-03-30", channel: "#giveaways", active: true },
  ]);

  const handleChange = (setter: Function) => (value: any) => {
    setter(value);
    setHasChanges(true);
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Giveaways</h1>
      <p className="text-[var(--muted)] mb-6">
        Manage server giveaways. Create, track, and configure giveaway settings for your community.
      </p>

      <div className="space-y-6">
        <ConfigPanel
          title="Giveaway Settings"
          hasChanges={hasChanges}
          onSave={() => setHasChanges(false)}
          onDiscard={() => setHasChanges(false)}
        >
          <Toggle label="Enable Giveaways" checked={enabled} onChange={handleChange(setEnabled)} />
          <Toggle label="DM Winners" checked={dmWinners} onChange={handleChange(setDmWinners)} />
          <Toggle label="Require Role to Enter" checked={requireRole} onChange={handleChange(setRequireRole)} />
          <SelectField
            label="Default Duration"
            value={defaultDuration}
            onChange={handleChange(setDefaultDuration)}
            options={[
              { value: "1h", label: "1 Hour" },
              { value: "12h", label: "12 Hours" },
              { value: "24h", label: "24 Hours" },
              { value: "3d", label: "3 Days" },
              { value: "7d", label: "7 Days" },
            ]}
          />
        </ConfigPanel>

        <ConfigPanel title="Active Giveaways">
          {giveaways.length === 0 ? (
            <p className="text-[var(--muted)] text-sm">No active giveaways.</p>
          ) : (
            <div className="space-y-3">
              {giveaways.map((g) => (
                <div
                  key={g.id}
                  className="flex items-center justify-between p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]"
                >
                  <div>
                    <p className="text-sm font-medium">{g.prize}</p>
                    <p className="text-xs text-[var(--muted)]">
                      {g.winners} winner{g.winners > 1 ? "s" : ""} &middot; Ends {g.endsAt} &middot; {g.channel}
                    </p>
                  </div>
                  <span className="text-xs px-2 py-1 rounded-full bg-green-500/20 text-green-400">Active</span>
                </div>
              ))}
            </div>
          )}
        </ConfigPanel>
      </div>
    </div>
  );
}
