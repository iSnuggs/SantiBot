"use client";

import { useState } from "react";
import ConfigPanel, { Toggle, InputField, SelectField } from "@/components/ConfigPanel";

export default function EconomyPage() {
  const [enabled, setEnabled] = useState(true);
  const [currencyName, setCurrencyName] = useState("coins");
  const [currencySymbol, setCurrencySymbol] = useState("$");
  const [dailyAmount, setDailyAmount] = useState("100");
  const [workMin, setWorkMin] = useState("50");
  const [workMax, setWorkMax] = useState("200");
  const [workCooldown, setWorkCooldown] = useState("1h");
  const [robEnabled, setRobEnabled] = useState(false);
  const [gambling, setGambling] = useState(true);
  const [leaderboard, setLeaderboard] = useState(true);
  const [startingBalance, setStartingBalance] = useState("0");
  const [hasChanges, setHasChanges] = useState(false);

  const handleChange = (setter: Function) => (value: any) => {
    setter(value);
    setHasChanges(true);
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Economy</h1>
      <p className="text-[var(--muted)] mb-6">
        Configure the virtual economy system. Set currency, earnings, and features for your server.
      </p>

      <div className="space-y-6">
        <ConfigPanel
          title="Currency Settings"
          hasChanges={hasChanges}
          onSave={() => setHasChanges(false)}
          onDiscard={() => setHasChanges(false)}
        >
          <Toggle label="Enable Economy" checked={enabled} onChange={handleChange(setEnabled)} />
          <InputField label="Currency Name" value={currencyName} onChange={handleChange(setCurrencyName)} placeholder="coins" />
          <InputField label="Currency Symbol" value={currencySymbol} onChange={handleChange(setCurrencySymbol)} placeholder="$" />
          <InputField label="Starting Balance" value={startingBalance} onChange={handleChange(setStartingBalance)} placeholder="0" type="number" />
        </ConfigPanel>

        <ConfigPanel title="Earnings">
          <InputField label="Daily Reward" value={dailyAmount} onChange={handleChange(setDailyAmount)} placeholder="100" type="number" />
          <InputField label="Work Min Reward" value={workMin} onChange={handleChange(setWorkMin)} placeholder="50" type="number" />
          <InputField label="Work Max Reward" value={workMax} onChange={handleChange(setWorkMax)} placeholder="200" type="number" />
          <SelectField
            label="Work Cooldown"
            value={workCooldown}
            onChange={handleChange(setWorkCooldown)}
            options={[
              { value: "30m", label: "30 Minutes" },
              { value: "1h", label: "1 Hour" },
              { value: "2h", label: "2 Hours" },
              { value: "4h", label: "4 Hours" },
            ]}
          />
        </ConfigPanel>

        <ConfigPanel title="Features">
          <Toggle label="Allow Robbing" checked={robEnabled} onChange={handleChange(setRobEnabled)} />
          <Toggle label="Gambling Commands" checked={gambling} onChange={handleChange(setGambling)} />
          <Toggle label="Show Leaderboard" checked={leaderboard} onChange={handleChange(setLeaderboard)} />
        </ConfigPanel>
      </div>
    </div>
  );
}
