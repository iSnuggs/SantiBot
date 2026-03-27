"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel, { Toggle, InputField } from "@/components/ConfigPanel";

interface SettingsConfig {
  configured: boolean;
  prefix: string;
  timeZoneId: string;
  locale: string;
  deleteMessageOnCommand: boolean;
  autoAssignRoleIds: number[];
  disableGlobalExpressions: boolean;
}

export default function SettingsPage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const [prefix, setPrefix] = useState(".");
  const [timeZoneId, setTimeZoneId] = useState("");
  const [deleteMessageOnCommand, setDeleteMessageOnCommand] = useState(false);
  const [disableGlobalExpressions, setDisableGlobalExpressions] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);

  const [saved, setSaved] = useState({
    prefix: ".",
    timeZoneId: "",
    deleteMessageOnCommand: false,
    disableGlobalExpressions: false,
  });

  const fetchData = () => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<SettingsConfig>(`/api/guilds/${guildId}/config/settings`)
      .then((data) => {
        const state = {
          prefix: data.prefix || ".",
          timeZoneId: data.timeZoneId || "",
          deleteMessageOnCommand: data.deleteMessageOnCommand,
          disableGlobalExpressions: data.disableGlobalExpressions,
        };
        setPrefix(state.prefix);
        setTimeZoneId(state.timeZoneId);
        setDeleteMessageOnCommand(state.deleteMessageOnCommand);
        setDisableGlobalExpressions(state.disableGlobalExpressions);
        setSaved(state);
      })
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    fetchData();
  }, [guildId]);

  const handleChange = (setter: Function) => (value: any) => {
    setter(value);
    setHasChanges(true);
  };

  const handleSave = async () => {
    if (!guildId) return;
    setSaving(true);
    setError(null);
    try {
      await apiFetch(`/api/guilds/${guildId}/config/settings`, {
        method: "PATCH",
        body: JSON.stringify({
          prefix,
          timeZoneId,
          deleteMessageOnCommand,
          disableGlobalExpressions,
        }),
      });
      const newSaved = { prefix, timeZoneId, deleteMessageOnCommand, disableGlobalExpressions };
      setSaved(newSaved);
      setHasChanges(false);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleDiscard = () => {
    setPrefix(saved.prefix);
    setTimeZoneId(saved.timeZoneId);
    setDeleteMessageOnCommand(saved.deleteMessageOnCommand);
    setDisableGlobalExpressions(saved.disableGlobalExpressions);
    setHasChanges(false);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="w-8 h-8 border-4 border-[var(--accent)] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (error && !saved.prefix) {
    return (
      <div className="text-center py-20">
        <p className="text-red-400 mb-2">Failed to load server settings</p>
        <p className="text-sm text-[var(--muted)] mb-4">{error}</p>
        <button
          onClick={fetchData}
          className="px-4 py-2 text-sm rounded-lg bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white transition-colors"
        >
          Retry
        </button>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Server Settings</h1>
      <p className="text-[var(--muted)] mb-6">
        Configure general bot settings for this server including the command prefix,
        timezone, and global behavior toggles.
      </p>

      {error && (
        <div className="mb-4 p-3 rounded-lg bg-red-500/10 border border-red-500/30 text-red-400 text-sm">
          {error}
        </div>
      )}

      <ConfigPanel
        title="General Settings"
        description="Core configuration options for SantiBot in this server."
        hasChanges={hasChanges}
        onSave={handleSave}
        onDiscard={handleDiscard}
      >
        <InputField
          label="Command Prefix"
          value={prefix}
          onChange={handleChange(setPrefix)}
          placeholder="."
        />
        <InputField
          label="Timezone"
          value={timeZoneId}
          onChange={handleChange(setTimeZoneId)}
          placeholder="e.g. America/New_York"
        />
        <Toggle
          label="Delete Command Messages"
          checked={deleteMessageOnCommand}
          onChange={handleChange(setDeleteMessageOnCommand)}
        />
        <Toggle
          label="Disable Global Expressions"
          checked={disableGlobalExpressions}
          onChange={handleChange(setDisableGlobalExpressions)}
        />
      </ConfigPanel>
    </div>
  );
}
