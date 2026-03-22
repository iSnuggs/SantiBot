"use client";

import { useState } from "react";
import ConfigPanel, { Toggle, InputField, SelectField } from "@/components/ConfigPanel";

interface PermissionOverride {
  id: string;
  target: string;
  targetType: "role" | "user";
  command: string;
  permission: "allow" | "deny";
}

export default function PermissionsPage() {
  const [strictMode, setStrictMode] = useState(false);
  const [ignoreAdmins, setIgnoreAdmins] = useState(true);
  const [defaultAction, setDefaultAction] = useState("allow");
  const [errorMessage, setErrorMessage] = useState("");
  const [hasChanges, setHasChanges] = useState(false);

  const [overrides] = useState<PermissionOverride[]>([
    { id: "1", target: "@Moderator", targetType: "role", command: "/ban", permission: "allow" },
    { id: "2", target: "@Member", targetType: "role", command: "/purge", permission: "deny" },
    { id: "3", target: "@DJ", targetType: "role", command: "/music *", permission: "allow" },
    { id: "4", target: "@everyone", targetType: "role", command: "/economy reset", permission: "deny" },
  ]);

  const handleChange = (setter: Function) => (value: any) => {
    setter(value);
    setHasChanges(true);
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Permissions</h1>
      <p className="text-[var(--muted)] mb-6">
        Configure command permission overrides for roles and users. Control who can use specific bot commands.
      </p>

      <div className="space-y-6">
        <ConfigPanel
          title="Global Permission Settings"
          hasChanges={hasChanges}
          onSave={() => setHasChanges(false)}
          onDiscard={() => setHasChanges(false)}
        >
          <Toggle label="Strict Mode (deny by default)" checked={strictMode} onChange={handleChange(setStrictMode)} />
          <Toggle label="Ignore Permission Checks for Admins" checked={ignoreAdmins} onChange={handleChange(setIgnoreAdmins)} />
          <SelectField
            label="Default Action"
            value={defaultAction}
            onChange={handleChange(setDefaultAction)}
            options={[
              { value: "allow", label: "Allow" },
              { value: "deny", label: "Deny" },
            ]}
          />
          <InputField
            label="Custom Denied Message"
            value={errorMessage}
            onChange={handleChange(setErrorMessage)}
            placeholder="You do not have permission to use this command."
          />
        </ConfigPanel>

        <ConfigPanel title="Permission Overrides">
          {overrides.length === 0 ? (
            <p className="text-[var(--muted)] text-sm">No permission overrides configured.</p>
          ) : (
            <div className="space-y-3">
              {overrides.map((o) => (
                <div
                  key={o.id}
                  className="flex items-center justify-between p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]"
                >
                  <div>
                    <p className="text-sm font-medium">
                      <code className="text-[var(--accent)]">{o.command}</code>
                    </p>
                    <p className="text-xs text-[var(--muted)]">
                      {o.target} ({o.targetType})
                    </p>
                  </div>
                  <span
                    className={`text-xs px-2 py-1 rounded-full ${
                      o.permission === "allow"
                        ? "bg-green-500/20 text-green-400"
                        : "bg-red-500/20 text-red-400"
                    }`}
                  >
                    {o.permission === "allow" ? "Allow" : "Deny"}
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
