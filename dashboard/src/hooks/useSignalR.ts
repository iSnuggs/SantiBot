"use client";

import { useEffect, useRef, useCallback } from "react";
import { HubConnectionBuilder, HubConnection, LogLevel } from "@microsoft/signalr";
import { getToken } from "@/lib/api";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

/**
 * React hook that connects to the SantiBot SignalR hub for real-time updates.
 *
 * How it works:
 * 1. When you open a guild's config page, this hook connects to the hub
 * 2. It joins the "group" for that guild so it receives updates
 * 3. When someone else saves settings, the server sends a "ConfigUpdated" event
 * 4. The onConfigUpdated callback fires, telling the page which section changed
 * 5. The page can then re-fetch its data to show the latest settings
 *
 * Usage in a page:
 *   useSignalR(guildId, (section) => {
 *     if (section === "starboard") fetchData();
 *   });
 */
export function useSignalR(
  guildId: string | undefined,
  onConfigUpdated: (section: string) => void
) {
  const connectionRef = useRef<HubConnection | null>(null);
  const callbackRef = useRef(onConfigUpdated);

  // Keep the callback ref up to date without reconnecting
  useEffect(() => {
    callbackRef.current = onConfigUpdated;
  }, [onConfigUpdated]);

  useEffect(() => {
    if (!guildId) return;

    const token = getToken();
    if (!token) return;

    // Build the SignalR connection with JWT auth
    const connection = new HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/dashboard`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connectionRef.current = connection;

    // Start the connection and join the guild's group
    connection
      .start()
      .then(() => {
        connection.invoke("JoinGuild", guildId);
      })
      .catch(() => {
        // Connection failed — dashboard still works, just no live updates
      });

    // Listen for config changes from other users
    connection.on("ConfigUpdated", (section: string) => {
      callbackRef.current(section);
    });

    // Clean up: leave guild group and stop connection
    return () => {
      if (connection.state === "Connected") {
        connection.invoke("LeaveGuild", guildId).catch(() => {});
      }
      connection.stop();
    };
  }, [guildId]);
}
