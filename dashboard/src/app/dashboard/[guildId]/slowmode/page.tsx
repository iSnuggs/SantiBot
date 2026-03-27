"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

export default function SlowmodePage() {
  const { guildId } = useParams();

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Slowmode</h1>
      <p className="text-[var(--muted)] mb-6">
        Slowmode is managed per-channel using bot commands. It controls how often users can send
        messages in a channel.
      </p>

      <div className="space-y-6">
        <ConfigPanel
          title="How to Use Slowmode"
          description="Slowmode is applied directly to Discord channels via bot commands"
        >
          <div className="space-y-4">
            <div className="p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]">
              <p className="text-sm font-medium mb-1">Set slowmode on a channel</p>
              <code className="text-xs px-2 py-1 rounded bg-[var(--card)] border border-[var(--border)] text-[var(--accent)]">
                .slowmode #channel 5
              </code>
              <p className="text-xs text-[var(--muted)] mt-2">
                Sets a 5-second cooldown between messages in the specified channel.
              </p>
            </div>

            <div className="p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]">
              <p className="text-sm font-medium mb-1">Remove slowmode from a channel</p>
              <code className="text-xs px-2 py-1 rounded bg-[var(--card)] border border-[var(--border)] text-[var(--accent)]">
                .slowmode #channel 0
              </code>
              <p className="text-xs text-[var(--muted)] mt-2">
                Setting the delay to 0 removes slowmode entirely.
              </p>
            </div>
          </div>
        </ConfigPanel>

        <ConfigPanel title="Important Note">
          <div className="flex items-start gap-3 p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]">
            <div className="w-5 h-5 mt-0.5 shrink-0 flex items-center justify-center rounded-full bg-[var(--accent)]/20 text-[var(--accent)] text-xs font-bold">
              i
            </div>
            <p className="text-sm text-[var(--muted)]">
              Channel slowmode settings are applied directly to Discord channels and can be viewed in
              Discord&apos;s channel settings. This page is for reference only &mdash; slowmode
              values cannot be changed from the dashboard.
            </p>
          </div>
        </ConfigPanel>
      </div>
    </div>
  );
}
