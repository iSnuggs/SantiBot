"use client";
import ConfigPanel, { Toggle } from "@/components/ConfigPanel";
import { useState } from "react";

export default function ModerationPage() {
  const [antiSpam, setAntiSpam] = useState(false);
  const [antiRaid, setAntiRaid] = useState(false);
  const [antiAlt, setAntiAlt] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);
  const change = (fn: Function) => (v: any) => { fn(v); setHasChanges(true); };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Moderation</h1>
      <p className="text-[var(--muted)] mb-6">Configure auto-moderation and server protection features.</p>
      <ConfigPanel title="Protection" hasChanges={hasChanges} onSave={() => setHasChanges(false)} onDiscard={() => setHasChanges(false)}>
        <Toggle label="Anti-Spam" checked={antiSpam} onChange={change(setAntiSpam)} />
        <Toggle label="Anti-Raid" checked={antiRaid} onChange={change(setAntiRaid)} />
        <Toggle label="Anti-Alt Account" checked={antiAlt} onChange={change(setAntiAlt)} />
      </ConfigPanel>
    </div>
  );
}
