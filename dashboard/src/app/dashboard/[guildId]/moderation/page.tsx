"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel, { Toggle, InputField } from "@/components/ConfigPanel";

interface ModerationConfig {
  configured: boolean;
  prefix: string;
  deleteMessageOnCommand: boolean;
  warnExpireHours: number;
  warnExpireAction: string;
  muteRoleName: string;
  verboseErrors: boolean;
  verbosePermissions: boolean;
  stickyRoles: boolean;
  timeZoneId: string;
}

export default function ModerationPage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);

  const [deleteMessageOnCommand, setDeleteMessageOnCommand] = useState(false);
  const [verboseErrors, setVerboseErrors] = useState(false);
  const [stickyRoles, setStickyRoles] = useState(false);
  const [muteRoleName, setMuteRoleName] = useState("");
  const [warnExpireHours, setWarnExpireHours] = useState("");

  // Store original values for discard
  const [original, setOriginal] = useState({
    deleteMessageOnCommand: false,
    verboseErrors: false,
    stickyRoles: false,
    muteRoleName: "",
    warnExpireHours: "",
  });

  useEffect(() => {
    if (!guildId) return;

    setLoading(true);
    apiFetch<ModerationConfig>(`/api/guilds/${guildId}/config/moderation`)
      .then((res) => {
        setDeleteMessageOnCommand(res.deleteMessageOnCommand);
        setVerboseErrors(res.verboseErrors);
        setStickyRoles(res.stickyRoles);
        setMuteRoleName(res.muteRoleName || "");
        setWarnExpireHours(String(res.warnExpireHours ?? ""));
        setOriginal({
          deleteMessageOnCommand: res.deleteMessageOnCommand,
          verboseErrors: res.verboseErrors,
          stickyRoles: res.stickyRoles,
          muteRoleName: res.muteRoleName || "",
          warnExpireHours: String(res.warnExpireHours ?? ""),
        });
        setError(null);
      })
      .catch((err) => {
        setError(err.message || "Failed to load moderation config");
      })
      .finally(() => setLoading(false));
  }, [guildId]);

  const change = <T,>(setter: (v: T) => void) => (v: T) => {
    setter(v);
    setHasChanges(true);
  };

  const handleDiscard = () => {
    setDeleteMessageOnCommand(original.deleteMessageOnCommand);
    setVerboseErrors(original.verboseErrors);
    setStickyRoles(original.stickyRoles);
    setMuteRoleName(original.muteRoleName);
    setWarnExpireHours(original.warnExpireHours);
    setHasChanges(false);
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      await apiFetch(`/api/guilds/${guildId}/config/moderation`, {
        method: "PATCH",
        body: JSON.stringify({
          deleteMessageOnCommand,
          verboseErrors,
          stickyRoles,
          muteRoleName,
          warnExpireHours: warnExpireHours ? Number(warnExpireHours) : 0,
        }),
      });
      setOriginal({
        deleteMessageOnCommand,
        verboseErrors,
        stickyRoles,
        muteRoleName,
        warnExpireHours,
      });
      setHasChanges(false);
      setError(null);
    } catch (err: any) {
      setError(err.message || "Failed to save moderation config");
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="animate-spin rounded-full h-8 w-8 border-2 border-[var(--accent)] border-t-transparent" />
      </div>
    );
  }

  if (error && !saving) {
    // Show error banner but still render the form if we have data
    // If initial load failed, show error only
    if (!original.muteRoleName && !original.deleteMessageOnCommand && !original.verboseErrors && !original.stickyRoles && !original.warnExpireHours) {
      return (
        <div className="bg-red-500/10 border border-red-500/30 rounded-xl p-6 text-center">
          <p className="text-red-400 font-medium">Failed to load moderation settings</p>
          <p className="text-red-400/70 text-sm mt-1">{error}</p>
        </div>
      );
    }
  }

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Moderation</h1>
      <p className="text-[var(--muted)] mb-6">
        Configure auto-moderation and server protection features.
      </p>

      {error && (
        <div className="bg-red-500/10 border border-red-500/30 rounded-xl p-4 mb-6">
          <p className="text-red-400 text-sm">{error}</p>
        </div>
      )}

      <ConfigPanel
        title="Moderation Settings"
        hasChanges={hasChanges}
        onSave={handleSave}
        onDiscard={handleDiscard}
      >
        <Toggle
          label="Delete Command Messages"
          checked={deleteMessageOnCommand}
          onChange={change(setDeleteMessageOnCommand)}
        />
        <Toggle
          label="Verbose Errors"
          checked={verboseErrors}
          onChange={change(setVerboseErrors)}
        />
        <Toggle
          label="Sticky Roles"
          checked={stickyRoles}
          onChange={change(setStickyRoles)}
        />
        <InputField
          label="Mute Role Name"
          value={muteRoleName}
          onChange={change(setMuteRoleName)}
          placeholder="e.g. Muted"
        />
        <InputField
          label="Warning Expiry Hours"
          value={warnExpireHours}
          onChange={change(setWarnExpireHours)}
          placeholder="e.g. 720"
          type="number"
        />
      </ConfigPanel>
    </div>
  );
}
