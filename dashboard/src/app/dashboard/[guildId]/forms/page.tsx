"use client";

import { useState } from "react";
import ConfigPanel, { Toggle, InputField, SelectField } from "@/components/ConfigPanel";

interface Form {
  id: string;
  name: string;
  submissions: number;
  status: "open" | "closed";
  responseChannel: string;
}

export default function FormsPage() {
  const [enabled, setEnabled] = useState(true);
  const [dmConfirmation, setDmConfirmation] = useState(true);
  const [logChannel, setLogChannel] = useState("");
  const [cooldown, setCooldown] = useState("none");
  const [hasChanges, setHasChanges] = useState(false);

  const [forms] = useState<Form[]>([
    { id: "1", name: "Staff Application", submissions: 12, status: "open", responseChannel: "#staff-apps" },
    { id: "2", name: "Bug Report", submissions: 34, status: "open", responseChannel: "#bug-reports" },
    { id: "3", name: "Event Suggestion", submissions: 8, status: "closed", responseChannel: "#suggestions" },
  ]);

  const handleChange = (setter: Function) => (value: any) => {
    setter(value);
    setHasChanges(true);
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Forms</h1>
      <p className="text-[var(--muted)] mb-6">
        Create and manage custom forms for applications, reports, and feedback from your server members.
      </p>

      <div className="space-y-6">
        <ConfigPanel
          title="Form Settings"
          hasChanges={hasChanges}
          onSave={() => setHasChanges(false)}
          onDiscard={() => setHasChanges(false)}
        >
          <Toggle label="Enable Forms" checked={enabled} onChange={handleChange(setEnabled)} />
          <Toggle label="DM Submission Confirmation" checked={dmConfirmation} onChange={handleChange(setDmConfirmation)} />
          <InputField
            label="Log Channel ID"
            value={logChannel}
            onChange={handleChange(setLogChannel)}
            placeholder="Enter channel ID for form logs"
          />
          <SelectField
            label="Submission Cooldown"
            value={cooldown}
            onChange={handleChange(setCooldown)}
            options={[
              { value: "none", label: "No Cooldown" },
              { value: "5m", label: "5 Minutes" },
              { value: "1h", label: "1 Hour" },
              { value: "24h", label: "24 Hours" },
            ]}
          />
        </ConfigPanel>

        <ConfigPanel title="Managed Forms">
          {forms.length === 0 ? (
            <p className="text-[var(--muted)] text-sm">No forms created yet.</p>
          ) : (
            <div className="space-y-3">
              {forms.map((f) => (
                <div
                  key={f.id}
                  className="flex items-center justify-between p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]"
                >
                  <div>
                    <p className="text-sm font-medium">{f.name}</p>
                    <p className="text-xs text-[var(--muted)]">
                      {f.submissions} submissions &middot; {f.responseChannel}
                    </p>
                  </div>
                  <span
                    className={`text-xs px-2 py-1 rounded-full ${
                      f.status === "open"
                        ? "bg-green-500/20 text-green-400"
                        : "bg-red-500/20 text-red-400"
                    }`}
                  >
                    {f.status === "open" ? "Open" : "Closed"}
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
