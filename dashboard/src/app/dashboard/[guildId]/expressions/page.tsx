"use client";

import { useState } from "react";
import ConfigPanel, { Toggle, SelectField } from "@/components/ConfigPanel";

interface Expression {
  id: string;
  trigger: string;
  response: string;
  type: "exact" | "contains" | "regex";
  uses: number;
}

export default function ExpressionsPage() {
  const [enabled, setEnabled] = useState(true);
  const [ignorePrefix, setIgnorePrefix] = useState(true);
  const [caseSensitive, setCaseSensitive] = useState(false);
  const [defaultType, setDefaultType] = useState("contains");
  const [hasChanges, setHasChanges] = useState(false);

  const [expressions] = useState<Expression[]>([
    { id: "1", trigger: "hello", response: "Hey there! Welcome to the server!", type: "exact", uses: 142 },
    { id: "2", trigger: "rules", response: "Please check #rules for server guidelines.", type: "contains", uses: 89 },
    { id: "3", trigger: "gg|good game", response: "Well played!", type: "regex", uses: 56 },
  ]);

  const handleChange = (setter: Function) => (value: any) => {
    setter(value);
    setHasChanges(true);
  };

  const typeLabel = (t: string) => {
    switch (t) {
      case "exact": return "Exact Match";
      case "contains": return "Contains";
      case "regex": return "Regex";
      default: return t;
    }
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Custom Expressions</h1>
      <p className="text-[var(--muted)] mb-6">
        Set up automatic responses triggered by specific words or patterns in messages.
      </p>

      <div className="space-y-6">
        <ConfigPanel
          title="Expression Settings"
          hasChanges={hasChanges}
          onSave={() => setHasChanges(false)}
          onDiscard={() => setHasChanges(false)}
        >
          <Toggle label="Enable Expressions" checked={enabled} onChange={handleChange(setEnabled)} />
          <Toggle label="Ignore Bot Prefix" checked={ignorePrefix} onChange={handleChange(setIgnorePrefix)} />
          <Toggle label="Case Sensitive" checked={caseSensitive} onChange={handleChange(setCaseSensitive)} />
          <SelectField
            label="Default Match Type"
            value={defaultType}
            onChange={handleChange(setDefaultType)}
            options={[
              { value: "exact", label: "Exact Match" },
              { value: "contains", label: "Contains" },
              { value: "regex", label: "Regex" },
            ]}
          />
        </ConfigPanel>

        <ConfigPanel title="Expression List">
          {expressions.length === 0 ? (
            <p className="text-[var(--muted)] text-sm">No expressions configured.</p>
          ) : (
            <div className="space-y-3">
              {expressions.map((expr) => (
                <div
                  key={expr.id}
                  className="p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]"
                >
                  <div className="flex items-center justify-between mb-1">
                    <code className="text-sm font-mono text-[var(--accent)]">{expr.trigger}</code>
                    <span className="text-xs px-2 py-0.5 rounded-full bg-[var(--border)] text-[var(--muted)]">
                      {typeLabel(expr.type)}
                    </span>
                  </div>
                  <p className="text-xs text-[var(--muted)]">{expr.response}</p>
                  <p className="text-xs text-[var(--muted)] mt-1">Used {expr.uses} times</p>
                </div>
              ))}
            </div>
          )}
        </ConfigPanel>
      </div>
    </div>
  );
}
