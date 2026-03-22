import type { InstallerState } from "../App";

interface Props {
  state: InstallerState;
  update: (partial: Partial<InstallerState>) => void;
  onNext: () => void;
  onBack: () => void;
}

export default function Configuration({ state, update, onNext, onBack }: Props) {
  const defaultPath = state.deployMethod === "docker"
    ? "~/santibot"
    : "~/SantiBot";

  return (
    <div className="max-w-lg mx-auto">
      <h2 className="text-2xl font-bold mb-2">Configuration</h2>
      <p className="text-[var(--muted)] text-sm mb-6">
        Configure your SantiBot installation.
      </p>

      <div className="bg-[var(--card)] border border-[var(--border)] rounded-xl p-6 space-y-4 mb-6">
        <div>
          <label className="block text-sm font-medium mb-1.5">Install Path</label>
          <input
            value={state.installPath}
            onChange={(e) => update({ installPath: e.target.value })}
            placeholder={defaultPath}
            className="w-full px-3 py-2 bg-[var(--background)] border border-[var(--border)] rounded-lg text-sm focus:border-[var(--accent)] focus:outline-none"
          />
          <p className="text-xs text-[var(--muted)] mt-1">Leave empty for default: {defaultPath}</p>
        </div>

        <div>
          <label className="block text-sm font-medium mb-1.5">Command Prefix</label>
          <input
            value={state.prefix}
            onChange={(e) => update({ prefix: e.target.value })}
            placeholder="."
            className="w-full px-3 py-2 bg-[var(--background)] border border-[var(--border)] rounded-lg text-sm focus:border-[var(--accent)] focus:outline-none"
          />
        </div>

        <div>
          <label className="block text-sm font-medium mb-1.5">Owner Discord ID</label>
          <input
            value={state.ownerId}
            onChange={(e) => update({ ownerId: e.target.value })}
            placeholder="Your Discord User ID"
            className="w-full px-3 py-2 bg-[var(--background)] border border-[var(--border)] rounded-lg text-sm focus:border-[var(--accent)] focus:outline-none"
          />
          <p className="text-xs text-[var(--muted)] mt-1">
            Right-click your name in Discord → Copy User ID (enable Developer Mode first)
          </p>
        </div>

        <div className="flex items-center justify-between py-2">
          <div>
            <span className="text-sm font-medium">Enable Dashboard</span>
            <p className="text-xs text-[var(--muted)]">Web-based management UI</p>
          </div>
          <button
            onClick={() => update({ enableDashboard: !state.enableDashboard })}
            className={`relative w-11 h-6 rounded-full transition-colors ${
              state.enableDashboard ? "bg-[var(--accent)]" : "bg-[var(--border)]"
            }`}
          >
            <span className={`absolute top-0.5 left-0.5 w-5 h-5 rounded-full bg-white transition-transform ${
              state.enableDashboard ? "translate-x-5" : ""
            }`} />
          </button>
        </div>
      </div>

      <div className="flex justify-between">
        <button onClick={onBack} className="px-4 py-2 text-sm border border-[var(--border)] rounded-lg hover:bg-[var(--card)]">
          Back
        </button>
        <button onClick={onNext} className="px-6 py-2 bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white text-sm rounded-lg">
          Next
        </button>
      </div>
    </div>
  );
}
