"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface BackupData {
  version: string;
  guildId: string;
  exportedAt: string;
  starboard: any | null;
  logging: any | null;
  moderation: any | null;
  music: any | null;
  expressions: any[];
  autoPurge: any[];
  permissions: any[];
}

export default function BackupPage() {
  const { guildId } = useParams();
  const [exporting, setExporting] = useState(false);
  const [importing, setImporting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [preview, setPreview] = useState<BackupData | null>(null);
  const [importFile, setImportFile] = useState<File | null>(null);

  // ── Export ──
  const handleExport = async () => {
    if (!guildId) return;
    setExporting(true);
    setError(null);
    setSuccess(null);

    try {
      const data = await apiFetch<BackupData>(`/api/guilds/${guildId}/backup`);

      // Download as JSON file
      const blob = new Blob([JSON.stringify(data, null, 2)], {
        type: "application/json",
      });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `santibot-backup-${guildId}-${new Date().toISOString().slice(0, 10)}.json`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);

      setSuccess("Backup exported successfully!");
    } catch (err: any) {
      setError(err.message);
    } finally {
      setExporting(false);
    }
  };

  // ── File Upload ──
  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setImportFile(file);
    setError(null);
    setSuccess(null);

    const reader = new FileReader();
    reader.onload = (ev) => {
      try {
        const data = JSON.parse(ev.target?.result as string) as BackupData;
        setPreview(data);
      } catch {
        setError("Invalid JSON file. Please select a valid SantiBot backup.");
        setPreview(null);
      }
    };
    reader.readAsText(file);
  };

  // ── Restore ──
  const handleRestore = async () => {
    if (!guildId || !preview) return;
    setImporting(true);
    setError(null);
    setSuccess(null);

    try {
      const result = await apiFetch<{ success: boolean; sectionsRestored: string[] }>(
        `/api/guilds/${guildId}/restore`,
        {
          method: "POST",
          body: JSON.stringify(preview),
        }
      );

      setSuccess(
        `Restore complete! Sections restored: ${result.sectionsRestored.join(", ") || "none"}`
      );
      setPreview(null);
      setImportFile(null);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setImporting(false);
    }
  };

  const handleCancelImport = () => {
    setPreview(null);
    setImportFile(null);
    setError(null);
  };

  // Count sections in a backup
  const countSections = (data: BackupData) => {
    let count = 0;
    if (data.starboard) count++;
    if (data.logging) count++;
    if (data.moderation) count++;
    if (data.music) count++;
    if (data.expressions?.length > 0) count++;
    if (data.autoPurge?.length > 0) count++;
    if (data.permissions?.length > 0) count++;
    return count;
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Backup & Restore</h1>
      <p className="text-[var(--muted)] mb-6">
        Export all your server settings to a JSON file, or import settings from a previous backup.
        This is useful for migrating between servers or recovering from accidental changes.
      </p>

      {/* Status Messages */}
      {error && (
        <div className="mb-4 p-3 rounded-lg bg-red-500/10 border border-red-500/30 text-red-400 text-sm">
          {error}
        </div>
      )}
      {success && (
        <div className="mb-4 p-3 rounded-lg bg-green-500/10 border border-green-500/30 text-green-400 text-sm">
          {success}
        </div>
      )}

      <div className="space-y-6">
        {/* Export Panel */}
        <ConfigPanel title="Export Settings" description="Download a complete backup of all your server's SantiBot configuration.">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm">
                This will export starboard, logging, moderation, music, expressions,
                auto-purge, and permission settings.
              </p>
              <p className="text-[var(--muted)] text-xs mt-1">
                The backup file does not include user data (XP, economy balances, etc).
              </p>
            </div>
            <button
              onClick={handleExport}
              disabled={exporting}
              className="px-4 py-2 text-sm rounded-lg bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white disabled:opacity-50 transition-colors whitespace-nowrap ml-4"
            >
              {exporting ? "Exporting..." : "Export Settings"}
            </button>
          </div>
        </ConfigPanel>

        {/* Import Panel */}
        <ConfigPanel title="Import Settings" description="Restore settings from a previously exported backup file.">
          {!preview ? (
            <div>
              <label className="block">
                <div className="border-2 border-dashed border-[var(--border)] rounded-lg p-8 text-center cursor-pointer hover:border-[var(--accent)] transition-colors">
                  <p className="text-sm mb-2">
                    Drop a backup file here or click to browse
                  </p>
                  <p className="text-[var(--muted)] text-xs">
                    Accepts .json files exported from SantiBot
                  </p>
                  <input
                    type="file"
                    accept=".json,application/json"
                    onChange={handleFileSelect}
                    className="hidden"
                  />
                </div>
              </label>
            </div>
          ) : (
            <div>
              {/* Preview */}
              <div className="bg-[var(--background)] rounded-lg p-4 mb-4">
                <h3 className="text-sm font-semibold mb-3">Import Preview</h3>
                <div className="grid grid-cols-2 gap-2 text-sm">
                  <div className="text-[var(--muted)]">Backup version:</div>
                  <div>{preview.version || "unknown"}</div>
                  <div className="text-[var(--muted)]">Source server:</div>
                  <div>{preview.guildId || "unknown"}</div>
                  <div className="text-[var(--muted)]">Exported at:</div>
                  <div>
                    {preview.exportedAt
                      ? new Date(preview.exportedAt).toLocaleString()
                      : "unknown"}
                  </div>
                  <div className="text-[var(--muted)]">Sections included:</div>
                  <div>{countSections(preview)}</div>
                </div>

                {/* Section breakdown */}
                <div className="mt-3 pt-3 border-t border-[var(--border)]">
                  <p className="text-xs text-[var(--muted)] mb-2">Sections that will be restored:</p>
                  <div className="flex flex-wrap gap-2">
                    {preview.starboard && (
                      <span className="px-2 py-1 bg-[var(--card)] rounded text-xs border border-[var(--border)]">
                        Starboard
                      </span>
                    )}
                    {preview.logging && (
                      <span className="px-2 py-1 bg-[var(--card)] rounded text-xs border border-[var(--border)]">
                        Logging
                      </span>
                    )}
                    {preview.moderation && (
                      <span className="px-2 py-1 bg-[var(--card)] rounded text-xs border border-[var(--border)]">
                        Moderation
                      </span>
                    )}
                    {preview.music && (
                      <span className="px-2 py-1 bg-[var(--card)] rounded text-xs border border-[var(--border)]">
                        Music
                      </span>
                    )}
                    {preview.expressions?.length > 0 && (
                      <span className="px-2 py-1 bg-[var(--card)] rounded text-xs border border-[var(--border)]">
                        Expressions ({preview.expressions.length})
                      </span>
                    )}
                    {preview.autoPurge?.length > 0 && (
                      <span className="px-2 py-1 bg-[var(--card)] rounded text-xs border border-[var(--border)]">
                        Auto Purge ({preview.autoPurge.length})
                      </span>
                    )}
                    {preview.permissions?.length > 0 && (
                      <span className="px-2 py-1 bg-[var(--card)] rounded text-xs border border-[var(--border)]">
                        Permissions ({preview.permissions.length})
                      </span>
                    )}
                  </div>
                </div>
              </div>

              {/* Warning */}
              <div className="mb-4 p-3 rounded-lg bg-yellow-500/10 border border-yellow-500/30 text-yellow-400 text-sm">
                This will overwrite your current settings for the included sections.
                Make sure to export a backup first if you want to keep your current config.
              </div>

              {/* Actions */}
              <div className="flex justify-end gap-3">
                <button
                  onClick={handleCancelImport}
                  className="px-4 py-2 text-sm rounded-lg border border-[var(--border)] hover:bg-[var(--card-hover)] transition-colors"
                >
                  Cancel
                </button>
                <button
                  onClick={handleRestore}
                  disabled={importing}
                  className="px-4 py-2 text-sm rounded-lg bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white disabled:opacity-50 transition-colors"
                >
                  {importing ? "Restoring..." : "Confirm Restore"}
                </button>
              </div>
            </div>
          )}
        </ConfigPanel>
      </div>
    </div>
  );
}
