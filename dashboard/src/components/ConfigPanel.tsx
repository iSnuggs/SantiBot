"use client";

import { useState } from "react";

interface ConfigPanelProps {
  title: string;
  description?: string;
  children: React.ReactNode;
  onSave?: () => void;
  onDiscard?: () => void;
  hasChanges?: boolean;
}

export default function ConfigPanel({
  title,
  description,
  children,
  onSave,
  onDiscard,
  hasChanges,
}: ConfigPanelProps) {
  return (
    <div className="bg-[var(--card)] rounded-xl border border-[var(--border)] overflow-hidden">
      <div className="p-6 border-b border-[var(--border)]">
        <h2 className="text-xl font-semibold">{title}</h2>
        {description && (
          <p className="text-[var(--muted)] text-sm mt-1">{description}</p>
        )}
      </div>

      <div className="p-6">{children}</div>

      {(onSave || onDiscard) && (
        <div className="px-6 py-4 border-t border-[var(--border)] bg-[var(--card-hover)] flex justify-end gap-3">
          {onDiscard && (
            <button
              onClick={onDiscard}
              disabled={!hasChanges}
              className="px-4 py-2 text-sm rounded-lg border border-[var(--border)] hover:bg-[var(--card)] disabled:opacity-50 transition-colors"
            >
              Discard
            </button>
          )}
          {onSave && (
            <button
              onClick={onSave}
              disabled={!hasChanges}
              className="px-4 py-2 text-sm rounded-lg bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white disabled:opacity-50 transition-colors"
            >
              Save Changes
            </button>
          )}
        </div>
      )}
    </div>
  );
}

export function Toggle({
  label,
  checked,
  onChange,
}: {
  label: string;
  checked: boolean;
  onChange: (v: boolean) => void;
}) {
  return (
    <label className="flex items-center justify-between py-2">
      <span className="text-sm">{label}</span>
      <button
        role="switch"
        aria-checked={checked}
        onClick={() => onChange(!checked)}
        className={`relative w-11 h-6 rounded-full transition-colors ${
          checked ? "bg-[var(--accent)]" : "bg-[var(--border)]"
        }`}
      >
        <span
          className={`absolute top-0.5 left-0.5 w-5 h-5 rounded-full bg-white transition-transform ${
            checked ? "translate-x-5" : ""
          }`}
        />
      </button>
    </label>
  );
}

export function InputField({
  label,
  value,
  onChange,
  placeholder,
  type = "text",
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
  type?: string;
}) {
  return (
    <div className="mb-4">
      <label className="block text-sm font-medium mb-1.5">{label}</label>
      <input
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="w-full px-3 py-2 bg-[var(--background)] border border-[var(--border)] rounded-lg text-sm focus:border-[var(--accent)] focus:outline-none transition-colors"
      />
    </div>
  );
}

export function SelectField({
  label,
  value,
  onChange,
  options,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  options: { value: string; label: string }[];
}) {
  return (
    <div className="mb-4">
      <label className="block text-sm font-medium mb-1.5">{label}</label>
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="w-full px-3 py-2 bg-[var(--background)] border border-[var(--border)] rounded-lg text-sm focus:border-[var(--accent)] focus:outline-none transition-colors"
      >
        {options.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </select>
    </div>
  );
}
