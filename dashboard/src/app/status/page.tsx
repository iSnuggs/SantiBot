"use client";

import { useEffect, useState } from "react";
import { apiFetch } from "@/lib/api";

interface BotStatus {
  online: boolean;
  guilds: number;
  uptime: string;
  version: string;
}

export default function StatusPage() {
  const [status, setStatus] = useState<BotStatus | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const fetchStatus = () => {
    apiFetch<BotStatus>("/api/status")
      .then((data) => {
        setStatus(data);
        setError(null);
        setLastUpdated(new Date());
      })
      .catch((err) => {
        setError(err.message);
        setStatus(null);
      });
  };

  useEffect(() => {
    fetchStatus();

    // Auto-refresh every 30 seconds
    const interval = setInterval(fetchStatus, 30000);
    return () => clearInterval(interval);
  }, []);

  return (
    <div className="min-h-screen bg-[var(--background)] flex items-center justify-center p-4">
      <div className="w-full max-w-sm bg-[var(--card)] rounded-xl border border-[var(--border)] overflow-hidden">
        {/* Header */}
        <div className="p-4 border-b border-[var(--border)] flex items-center gap-3">
          <img src="/santi-logo.png" alt="SantiBot" className="w-8 h-8 rounded-lg" />
          <span className="text-lg font-bold">
            <span className="text-[var(--accent)]">Santi</span>Bot
          </span>
        </div>

        {/* Status Content */}
        <div className="p-4 space-y-4">
          {error ? (
            <div className="text-center py-4">
              <div className="w-3 h-3 rounded-full bg-red-500 mx-auto mb-2" />
              <p className="text-red-400 text-sm">Unable to reach bot</p>
              <p className="text-[var(--muted)] text-xs mt-1">{error}</p>
            </div>
          ) : !status ? (
            <div className="flex items-center justify-center py-6">
              <div className="w-6 h-6 border-3 border-[var(--accent)] border-t-transparent rounded-full animate-spin" />
            </div>
          ) : (
            <>
              {/* Online/Offline indicator */}
              <div className="flex items-center gap-3">
                <div
                  className={`w-3 h-3 rounded-full ${
                    status.online ? "bg-green-500 shadow-[0_0_8px_rgba(34,197,94,0.5)]" : "bg-red-500"
                  }`}
                />
                <span className="text-sm font-medium">
                  {status.online ? "Online" : "Offline"}
                </span>
              </div>

              {/* Stats grid */}
              <div className="grid grid-cols-3 gap-3">
                <div className="bg-[var(--background)] rounded-lg p-3 text-center">
                  <p className="text-lg font-bold">{status.guilds}</p>
                  <p className="text-[var(--muted)] text-xs">Servers</p>
                </div>
                <div className="bg-[var(--background)] rounded-lg p-3 text-center">
                  <p className="text-lg font-bold text-[var(--accent)]">{status.uptime}</p>
                  <p className="text-[var(--muted)] text-xs">Uptime</p>
                </div>
                <div className="bg-[var(--background)] rounded-lg p-3 text-center">
                  <p className="text-lg font-bold">v{status.version}</p>
                  <p className="text-[var(--muted)] text-xs">Version</p>
                </div>
              </div>
            </>
          )}
        </div>

        {/* Footer */}
        <div className="px-4 py-2 border-t border-[var(--border)] flex items-center justify-between">
          <p className="text-[var(--muted)] text-xs">
            {lastUpdated
              ? `Updated ${lastUpdated.toLocaleTimeString()}`
              : "Loading..."}
          </p>
          <p className="text-[var(--muted)] text-xs">Auto-refreshes every 30s</p>
        </div>
      </div>
    </div>
  );
}
