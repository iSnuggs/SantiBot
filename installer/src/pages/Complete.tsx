import type { InstallerState } from "../App";

interface Props {
  state: InstallerState;
}

export default function Complete({ state }: Props) {
  const installPath = state.installPath || (state.deployMethod === "docker" ? "~/santibot" : "~/SantiBot");

  return (
    <div className="flex flex-col items-center justify-center h-full text-center">
      <div className="text-6xl mb-4">🎉</div>
      <h1 className="text-3xl font-bold mb-2">
        <span className="text-[var(--accent)]">Santi</span>Bot is Ready!
      </h1>
      <p className="text-[var(--muted)] mb-8">
        Your bot has been installed and configured.
      </p>

      <div className="bg-[var(--card)] border border-[var(--border)] rounded-xl p-6 max-w-md w-full mb-6 text-left">
        <h3 className="font-medium mb-3">Installation Details</h3>
        <div className="space-y-2 text-sm">
          <div className="flex justify-between">
            <span className="text-[var(--muted)]">Bot Name</span>
            <span>{state.botName || "SantiBot"}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-[var(--muted)]">Method</span>
            <span className="capitalize">{state.deployMethod}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-[var(--muted)]">Location</span>
            <span className="text-xs font-mono">{installPath}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-[var(--muted)]">Prefix</span>
            <span className="font-mono">{state.prefix}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-[var(--muted)]">Dashboard</span>
            <span>{state.enableDashboard ? "Enabled" : "Disabled"}</span>
          </div>
        </div>
      </div>

      <div className="bg-[var(--card)] border border-[var(--border)] rounded-xl p-6 max-w-md w-full mb-6 text-left">
        <h3 className="font-medium mb-3">Next Steps</h3>
        <ol className="text-sm text-[var(--muted)] space-y-2 list-decimal list-inside">
          <li>
            <strong className="text-[var(--foreground)]">Invite the bot</strong> to your Discord server
            using the OAuth2 URL from the Developer Portal
          </li>
          <li>
            <strong className="text-[var(--foreground)]">Start the bot</strong>
            {state.deployMethod === "docker"
              ? ` — run: docker compose up -d`
              : ` — run: dotnet run --project src/SantiBot`}
          </li>
          <li>
            <strong className="text-[var(--foreground)]">Test it</strong> — type{" "}
            <code className="text-[var(--accent)]">{state.prefix}help</code> in any channel
          </li>
        </ol>
      </div>

      <div className="flex gap-3">
        <a
          href="https://github.com/iSnuggs/SantiBot"
          target="_blank"
          rel="noopener"
          className="px-4 py-2 text-sm border border-[var(--border)] rounded-lg hover:bg-[var(--card)] transition-colors"
        >
          View on GitHub
        </a>
        <button className="px-6 py-2 bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white text-sm rounded-lg">
          Open Dashboard
        </button>
      </div>

      <p className="mt-8 text-xs text-[var(--muted)]">
        Thank you for choosing SantiBot! 🐾
      </p>
    </div>
  );
}
