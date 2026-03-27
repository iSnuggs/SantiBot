"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";

interface NavItem {
  name: string;
  href: string;
  icon: string;
  category: string;
}

const navItems: NavItem[] = [
  // General
  { name: "Overview", href: "/overview", icon: "📊", category: "General" },
  { name: "Settings", href: "/settings", icon: "⚙️", category: "General" },

  // Management — moderation & protection tools
  { name: "Moderation", href: "/moderation", icon: "🛡️", category: "Management" },
  { name: "Mod Cases", href: "/mod-cases", icon: "📂", category: "Management" },
  { name: "Automod", href: "/automod", icon: "🤖", category: "Management" },
  { name: "Autoban", href: "/autoban", icon: "🚫", category: "Management" },
  { name: "Anti-Raid", href: "/anti-raid", icon: "🏰", category: "Management" },
  { name: "Logging", href: "/logging", icon: "📝", category: "Management" },
  { name: "Permissions", href: "/permissions", icon: "🔒", category: "Management" },
  { name: "Slowmode", href: "/slowmode", icon: "🐌", category: "Management" },

  // Automation — automatic actions
  { name: "Auto-Responder", href: "/auto-responder", icon: "💬", category: "Automation" },
  { name: "Auto Purge", href: "/auto-purge", icon: "🧹", category: "Automation" },
  { name: "Auto Delete", href: "/auto-delete", icon: "🗑️", category: "Automation" },
  { name: "Auto Message", href: "/auto-message", icon: "📨", category: "Automation" },
  { name: "Autoroles", href: "/autoroles", icon: "🎭", category: "Automation" },

  // Community — engagement features
  { name: "Starboard", href: "/starboard", icon: "⭐", category: "Community" },
  { name: "Giveaways", href: "/giveaways", icon: "🎉", category: "Community" },
  { name: "Polls", href: "/polls", icon: "📊", category: "Community" },
  { name: "Forms", href: "/forms", icon: "📋", category: "Community" },
  { name: "Suggestions", href: "/suggestions", icon: "💡", category: "Community" },
  { name: "Tickets", href: "/tickets", icon: "🎫", category: "Community" },
  { name: "Reaction Roles", href: "/reaction-roles", icon: "🏷️", category: "Community" },
  { name: "Welcome", href: "/welcome", icon: "👋", category: "Community" },

  // Engagement — XP, expressions, reminders
  { name: "XP & Leveling", href: "/xp", icon: "⬆️", category: "Engagement" },
  { name: "Expressions", href: "/expressions", icon: "🔤", category: "Engagement" },
  { name: "Reminders", href: "/reminders", icon: "⏰", category: "Engagement" },

  // Fun — music, economy
  { name: "Music", href: "/music", icon: "🎵", category: "Fun" },
  { name: "Economy", href: "/economy", icon: "💰", category: "Fun" },

  // Tools — utilities
  { name: "Embed Builder", href: "/embed-builder", icon: "🎨", category: "Tools" },
  { name: "Voice Text", href: "/voice-text", icon: "🔊", category: "Tools" },
  { name: "Streams", href: "/streams", icon: "📺", category: "Tools" },
];

export default function Sidebar({ guildId }: { guildId: string }) {
  const pathname = usePathname();
  const [mobileOpen, setMobileOpen] = useState(false);

  const categories = [...new Set(navItems.map((i) => i.category))];

  const basePath = `/dashboard/${guildId}`;

  return (
    <>
      {/* Mobile hamburger */}
      <button
        onClick={() => setMobileOpen(!mobileOpen)}
        className="lg:hidden fixed top-4 left-4 z-50 p-2 bg-[var(--card)] rounded-lg border border-[var(--border)]"
      >
        <span className="text-xl">{mobileOpen ? "✕" : "☰"}</span>
      </button>

      {/* Sidebar */}
      <aside
        className={`fixed lg:static inset-y-0 left-0 z-40 w-64 bg-[var(--card)] border-r border-[var(--border)] overflow-y-auto transition-transform ${
          mobileOpen ? "translate-x-0" : "-translate-x-full lg:translate-x-0"
        }`}
      >
        {/* Logo */}
        <div className="p-4 border-b border-[var(--border)]">
          <Link href="/dashboard" className="flex items-center gap-3">
            <img src="/santi-logo.png" alt="Santi" className="w-8 h-8 rounded-lg" />
            <span className="text-xl font-bold">
              <span className="text-[var(--accent)]">Santi</span>Bot
            </span>
          </Link>
        </div>

        {/* Navigation */}
        <nav className="p-3">
          {categories.map((cat) => (
            <div key={cat} className="mb-4">
              <h3 className="text-xs font-semibold text-[var(--muted)] uppercase tracking-wider px-3 mb-2">
                {cat}
              </h3>
              {navItems
                .filter((i) => i.category === cat)
                .map((item) => {
                  const href = `${basePath}${item.href}`;
                  const active = pathname === href;
                  return (
                    <Link
                      key={item.name}
                      href={href}
                      onClick={() => setMobileOpen(false)}
                      className={`flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors mb-0.5 ${
                        active
                          ? "bg-[var(--accent)] text-white"
                          : "text-[var(--foreground)] hover:bg-[var(--card-hover)]"
                      }`}
                    >
                      <span>{item.icon}</span>
                      <span>{item.name}</span>
                    </Link>
                  );
                })}
            </div>
          ))}
        </nav>
      </aside>

      {/* Mobile overlay */}
      {mobileOpen && (
        <div
          className="fixed inset-0 bg-black/50 z-30 lg:hidden"
          onClick={() => setMobileOpen(false)}
        />
      )}
    </>
  );
}
