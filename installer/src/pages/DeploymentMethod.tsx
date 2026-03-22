import type { InstallerState, DeployMethod } from "../App";

interface Props {
  state: InstallerState;
  update: (partial: Partial<InstallerState>) => void;
  onNext: () => void;
  onBack: () => void;
}

const methods: { id: DeployMethod; name: string; icon: string; desc: string }[] = [
  { id: "local", name: "Local Install", icon: "💻", desc: "Install directly on this machine. Requires .NET 8 SDK." },
  { id: "docker", name: "Docker", icon: "🐳", desc: "Run in a Docker container. Requires Docker installed." },
  { id: "vps", name: "Remote VPS", icon: "☁️", desc: "Deploy to a remote server via SSH." },
];

export default function DeploymentMethod({ state, update, onNext, onBack }: Props) {
  return (
    <div className="max-w-lg mx-auto">
      <h2 className="text-2xl font-bold mb-2">Deployment Method</h2>
      <p className="text-[var(--muted)] text-sm mb-6">
        Choose how you want to run SantiBot.
      </p>

      <div className="space-y-3 mb-6">
        {methods.map((m) => (
          <button
            key={m.id}
            onClick={() => update({ deployMethod: m.id })}
            className={`w-full text-left p-4 rounded-xl border transition-colors ${
              state.deployMethod === m.id
                ? "border-[var(--accent)] bg-[var(--accent)]/10"
                : "border-[var(--border)] bg-[var(--card)] hover:border-[var(--accent)]/50"
            }`}
          >
            <div className="flex items-center gap-3">
              <span className="text-2xl">{m.icon}</span>
              <div>
                <div className="font-medium">{m.name}</div>
                <div className="text-xs text-[var(--muted)]">{m.desc}</div>
              </div>
            </div>
          </button>
        ))}
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
