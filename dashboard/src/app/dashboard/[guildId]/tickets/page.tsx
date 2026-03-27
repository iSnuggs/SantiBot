"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface TicketConfig {
  enabled: boolean;
  categoryId: string | null;
  logChannelId: string | null;
  supportRoleId: string | null;
  maxTicketsPerUser: number;
  welcomeMessage: string | null;
}

interface Ticket {
  id: number;
  ticketNumber: number;
  creatorUserId: string;
  claimedByUserId: string | null;
  channelId: string;
  status: "Open" | "Claimed" | "Closed";
  topic: string | null;
  createdAt: string;
  closedAt: string | null;
}

interface TicketsResponse {
  config: TicketConfig | null;
  tickets: Ticket[];
}

const statusColors: Record<string, string> = {
  Open: "bg-green-500/20 text-green-400 border-green-500/30",
  Claimed: "bg-blue-500/20 text-blue-400 border-blue-500/30",
  Closed: "bg-gray-500/20 text-gray-400 border-gray-500/30",
};

export default function TicketsPage() {
  const { guildId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [config, setConfig] = useState<TicketConfig | null>(null);
  const [tickets, setTickets] = useState<Ticket[]>([]);

  const fetchData = () => {
    if (!guildId) return;
    setLoading(true);
    setError(null);
    apiFetch<TicketsResponse>(`/api/guilds/${guildId}/config/tickets`)
      .then((data) => {
        setConfig(data.config);
        setTickets(data.tickets);
      })
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    fetchData();
  }, [guildId]);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="w-8 h-8 border-4 border-[var(--accent)] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-20">
        <p className="text-red-400 mb-2">Failed to load tickets</p>
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
      <h1 className="text-2xl font-bold mb-6">Tickets</h1>
      <p className="text-[var(--muted)] mb-6">
        Manage your support ticket system. Users can open tickets for private support conversations.
      </p>

      <div className="space-y-6">
        <ConfigPanel title="Ticket Configuration" description="Current ticket system settings">
          {config ? (
            <div className="space-y-3">
              <div className="flex items-center justify-between py-2">
                <span className="text-sm text-[var(--muted)]">Status</span>
                <span
                  className={`text-xs px-2 py-1 rounded-full border ${
                    config.enabled
                      ? "bg-green-500/20 text-green-400 border-green-500/30"
                      : "bg-gray-500/20 text-gray-400 border-gray-500/30"
                  }`}
                >
                  {config.enabled ? "Enabled" : "Disabled"}
                </span>
              </div>
              <div className="flex items-center justify-between py-2 border-t border-[var(--border)]">
                <span className="text-sm text-[var(--muted)]">Category ID</span>
                <span className="text-sm font-mono">{config.categoryId || "Not set"}</span>
              </div>
              <div className="flex items-center justify-between py-2 border-t border-[var(--border)]">
                <span className="text-sm text-[var(--muted)]">Log Channel ID</span>
                <span className="text-sm font-mono">{config.logChannelId || "Not set"}</span>
              </div>
              <div className="flex items-center justify-between py-2 border-t border-[var(--border)]">
                <span className="text-sm text-[var(--muted)]">Support Role ID</span>
                <span className="text-sm font-mono">{config.supportRoleId || "Not set"}</span>
              </div>
              <div className="flex items-center justify-between py-2 border-t border-[var(--border)]">
                <span className="text-sm text-[var(--muted)]">Max Tickets Per User</span>
                <span className="text-sm">{config.maxTicketsPerUser}</span>
              </div>
              {config.welcomeMessage && (
                <div className="pt-2 border-t border-[var(--border)]">
                  <span className="text-sm text-[var(--muted)] block mb-1">Welcome Message</span>
                  <p className="text-sm bg-[var(--background)] p-3 rounded-lg border border-[var(--border)]">
                    {config.welcomeMessage}
                  </p>
                </div>
              )}
            </div>
          ) : (
            <p className="text-[var(--muted)] text-sm">
              Tickets not configured. Use <code className="bg-[var(--background)] px-1.5 py-0.5 rounded text-xs">.ticket enable</code> to set up.
            </p>
          )}
        </ConfigPanel>

        <ConfigPanel title="Recent Tickets" description="Latest support tickets in this server">
          {tickets.length > 0 ? (
            <div className="space-y-2">
              {tickets.map((ticket) => (
                <div
                  key={ticket.id}
                  className="flex items-center justify-between p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]"
                >
                  <div className="flex items-center gap-3 min-w-0">
                    <span className="text-sm font-mono text-[var(--muted)] shrink-0">
                      #{ticket.ticketNumber}
                    </span>
                    <span className="text-sm truncate">
                      {ticket.topic || "No topic"}
                    </span>
                  </div>
                  <div className="flex items-center gap-3 shrink-0 ml-3">
                    <span className="text-xs text-[var(--muted)]">
                      {new Date(ticket.createdAt).toLocaleDateString()}
                    </span>
                    <span
                      className={`text-xs px-2 py-1 rounded-full border ${
                        statusColors[ticket.status] || statusColors.Closed
                      }`}
                    >
                      {ticket.status}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-[var(--muted)] text-sm">No tickets yet.</p>
          )}
        </ConfigPanel>
      </div>
    </div>
  );
}
