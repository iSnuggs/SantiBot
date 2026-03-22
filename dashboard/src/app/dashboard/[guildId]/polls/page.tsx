"use client";

import { useState } from "react";
import ConfigPanel, { Toggle, SelectField } from "@/components/ConfigPanel";

interface Poll {
  id: string;
  question: string;
  votes: number;
  endsAt: string;
  channel: string;
}

export default function PollsPage() {
  const [enabled, setEnabled] = useState(true);
  const [allowMultipleVotes, setAllowMultipleVotes] = useState(false);
  const [showResults, setShowResults] = useState(true);
  const [defaultDuration, setDefaultDuration] = useState("24h");
  const [hasChanges, setHasChanges] = useState(false);

  const [polls] = useState<Poll[]>([
    { id: "1", question: "What game should we play this weekend?", votes: 47, endsAt: "2026-03-23", channel: "#polls" },
    { id: "2", question: "Best movie of 2026?", votes: 23, endsAt: "2026-03-28", channel: "#general" },
  ]);

  const handleChange = (setter: Function) => (value: any) => {
    setter(value);
    setHasChanges(true);
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Polls</h1>
      <p className="text-[var(--muted)] mb-6">
        Create and manage polls for your server. Let members vote on topics and view real-time results.
      </p>

      <div className="space-y-6">
        <ConfigPanel
          title="Poll Settings"
          hasChanges={hasChanges}
          onSave={() => setHasChanges(false)}
          onDiscard={() => setHasChanges(false)}
        >
          <Toggle label="Enable Polls" checked={enabled} onChange={handleChange(setEnabled)} />
          <Toggle label="Allow Multiple Votes" checked={allowMultipleVotes} onChange={handleChange(setAllowMultipleVotes)} />
          <Toggle label="Show Results Before End" checked={showResults} onChange={handleChange(setShowResults)} />
          <SelectField
            label="Default Duration"
            value={defaultDuration}
            onChange={handleChange(setDefaultDuration)}
            options={[
              { value: "1h", label: "1 Hour" },
              { value: "6h", label: "6 Hours" },
              { value: "24h", label: "24 Hours" },
              { value: "3d", label: "3 Days" },
              { value: "7d", label: "7 Days" },
            ]}
          />
        </ConfigPanel>

        <ConfigPanel title="Active Polls">
          {polls.length === 0 ? (
            <p className="text-[var(--muted)] text-sm">No active polls.</p>
          ) : (
            <div className="space-y-3">
              {polls.map((p) => (
                <div
                  key={p.id}
                  className="flex items-center justify-between p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]"
                >
                  <div>
                    <p className="text-sm font-medium">{p.question}</p>
                    <p className="text-xs text-[var(--muted)]">
                      {p.votes} votes &middot; Ends {p.endsAt} &middot; {p.channel}
                    </p>
                  </div>
                  <span className="text-xs px-2 py-1 rounded-full bg-blue-500/20 text-blue-400">Active</span>
                </div>
              ))}
            </div>
          )}
        </ConfigPanel>
      </div>
    </div>
  );
}
