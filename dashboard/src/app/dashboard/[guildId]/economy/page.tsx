"use client";

import { useState } from "react";
import ConfigPanel, { Toggle, InputField, SelectField } from "@/components/ConfigPanel";

export default function EconomyPage() {
  const [enabled] = useState(true);
  const [currencyName] = useState("coins");
  const [currencySymbol] = useState("$");
  const [dailyAmount] = useState("100");
  const [workMin] = useState("50");
  const [workMax] = useState("200");
  const [workCooldown] = useState("1h");
  const [robEnabled] = useState(false);
  const [gambling] = useState(true);
  const [leaderboard] = useState(true);
  const [startingBalance] = useState("0");

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Economy</h1>
      <p className="text-[var(--muted)] mb-6">
        Configure the virtual economy system. Set currency, earnings, and features for your server.
      </p>

      <div className="bg-[var(--accent)]/10 border border-[var(--accent)]/30 rounded-xl p-4 mb-6">
        <p className="text-sm text-[var(--accent)]">
          Economy settings are configured globally via bot commands. This page shows the default configuration.
        </p>
      </div>

      <div className="space-y-6">
        <ConfigPanel title="Currency Settings">
          <Toggle label="Enable Economy" checked={enabled} onChange={() => {}} />
          <InputField label="Currency Name" value={currencyName} onChange={() => {}} placeholder="coins" />
          <InputField label="Currency Symbol" value={currencySymbol} onChange={() => {}} placeholder="$" />
          <InputField label="Starting Balance" value={startingBalance} onChange={() => {}} placeholder="0" type="number" />
        </ConfigPanel>

        <ConfigPanel title="Earnings">
          <InputField label="Daily Reward" value={dailyAmount} onChange={() => {}} placeholder="100" type="number" />
          <InputField label="Work Min Reward" value={workMin} onChange={() => {}} placeholder="50" type="number" />
          <InputField label="Work Max Reward" value={workMax} onChange={() => {}} placeholder="200" type="number" />
          <SelectField
            label="Work Cooldown"
            value={workCooldown}
            onChange={() => {}}
            options={[
              { value: "30m", label: "30 Minutes" },
              { value: "1h", label: "1 Hour" },
              { value: "2h", label: "2 Hours" },
              { value: "4h", label: "4 Hours" },
            ]}
          />
        </ConfigPanel>

        <ConfigPanel title="Features">
          <Toggle label="Allow Robbing" checked={robEnabled} onChange={() => {}} />
          <Toggle label="Gambling Commands" checked={gambling} onChange={() => {}} />
          <Toggle label="Show Leaderboard" checked={leaderboard} onChange={() => {}} />
        </ConfigPanel>
      </div>
    </div>
  );
}
