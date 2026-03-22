"use client";

import { useState } from "react";
import ConfigPanel, { Toggle, InputField, SelectField } from "@/components/ConfigPanel";

export default function XPPage() {
  const [enabled, setEnabled] = useState(true);
  const [levelUpMessages, setLevelUpMessages] = useState(true);
  const [voiceXP, setVoiceXP] = useState(false);
  const [stackRoles, setStackRoles] = useState(true);
  const [xpRate, setXpRate] = useState("1x");
  const [minXP, setMinXP] = useState("15");
  const [maxXP, setMaxXP] = useState("25");
  const [cooldown, setCooldown] = useState("60");
  const [levelUpChannel, setLevelUpChannel] = useState("");
  const [hasChanges, setHasChanges] = useState(false);

  const handleChange = (setter: Function) => (value: any) => {
    setter(value);
    setHasChanges(true);
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">XP &amp; Leveling</h1>
      <p className="text-[var(--muted)] mb-6">
        Configure the XP and leveling system. Reward active members with levels and role rewards.
      </p>

      <div className="space-y-6">
        <ConfigPanel
          title="General Settings"
          hasChanges={hasChanges}
          onSave={() => setHasChanges(false)}
          onDiscard={() => setHasChanges(false)}
        >
          <Toggle label="Enable XP System" checked={enabled} onChange={handleChange(setEnabled)} />
          <Toggle label="Level-Up Announcements" checked={levelUpMessages} onChange={handleChange(setLevelUpMessages)} />
          <Toggle label="Voice Channel XP" checked={voiceXP} onChange={handleChange(setVoiceXP)} />
          <Toggle label="Stack Level Roles" checked={stackRoles} onChange={handleChange(setStackRoles)} />
          <InputField
            label="Level-Up Channel ID"
            value={levelUpChannel}
            onChange={handleChange(setLevelUpChannel)}
            placeholder="Leave empty for current channel"
          />
        </ConfigPanel>

        <ConfigPanel title="XP Rates">
          <SelectField
            label="XP Multiplier"
            value={xpRate}
            onChange={handleChange(setXpRate)}
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
            onChange={handleChange(setMinXP)}
            placeholder="15"
            type="number"
          />
          <InputField
            label="Max XP Per Message"
            value={maxXP}
            onChange={handleChange(setMaxXP)}
            placeholder="25"
            type="number"
          />
          <InputField
            label="Cooldown (seconds)"
            value={cooldown}
            onChange={handleChange(setCooldown)}
            placeholder="60"
            type="number"
          />
        </ConfigPanel>
      </div>
    </div>
  );
}
