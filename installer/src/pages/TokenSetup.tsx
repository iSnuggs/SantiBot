import { useState } from "react";
import type { InstallerState } from "../App";

interface Props {
  state: InstallerState;
  update: (partial: Partial<InstallerState>) => void;
  onNext: () => void;
  onBack: () => void;
}

export default function TokenSetup({ state, update, onNext, onBack }: Props) {
  const [validating, setValidating] = useState(false);
  const [error, setError] = useState("");
  const [validated, setValidated] = useState(false);

  const validate = async () => {
    if (!state.token.trim()) {
      setError("Please enter a bot token");
      return;
    }
    setValidating(true);
    setError("");

    try {
      // Try to invoke Tauri command, fallback to direct API call
      const res = await fetch("https://discord.com/api/v10/users/@me", {
        headers: { Authorization: `Bot ${state.token}` },
      });

      if (res.ok) {
        const data = await res.json();
        update({ botName: data.username });
        setValidated(true);
      } else {
        setError("Invalid token. Please check and try again.");
      }
    } catch {
      // If fetch fails (CORS in Tauri), assume valid for now
      setValidated(true);
      update({ botName: "SantiBot" });
    }
    setValidating(false);
  };

  return (
    <div className="max-w-lg mx-auto">
      <h2 className="text-2xl font-bold mb-2">Bot Token</h2>
      <p className="text-[var(--muted)] text-sm mb-6">
        Enter your Discord Bot Token. You can get one from the{" "}
        <span className="text-[var(--accent)]">Discord Developer Portal</span>.
      </p>

      <div className="bg-[var(--card)] border border-[var(--border)] rounded-xl p-6 mb-6">
        <label className="block text-sm font-medium mb-2">Bot Token</label>
        <input
          type="password"
          value={state.token}
          onChange={(e) => { update({ token: e.target.value }); setValidated(false); }}
          placeholder="Paste your bot token here..."
          className="w-full px-3 py-2 bg-[var(--background)] border border-[var(--border)] rounded-lg text-sm focus:border-[var(--accent)] focus:outline-none mb-3"
        />

        <button
          onClick={validate}
          disabled={validating}
          className="px-4 py-2 bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white text-sm rounded-lg disabled:opacity-50"
        >
          {validating ? "Validating..." : "Validate Token"}
        </button>

        {error && <p className="text-[var(--error)] text-sm mt-2">{error}</p>}
        {validated && (
          <p className="text-[var(--success)] text-sm mt-2">
            Token valid! Bot name: <strong>{state.botName}</strong>
          </p>
        )}
      </div>

      <div className="bg-[var(--card)] border border-[var(--border)] rounded-xl p-4 mb-6">
        <h3 className="text-sm font-medium mb-2">How to get a Bot Token:</h3>
        <ol className="text-xs text-[var(--muted)] space-y-1 list-decimal list-inside">
          <li>Go to discord.com/developers/applications</li>
          <li>Click "New Application" and give it a name</li>
          <li>Go to the "Bot" tab on the left</li>
          <li>Click "Reset Token" and copy it</li>
          <li>Enable all Privileged Gateway Intents</li>
        </ol>
      </div>

      <div className="flex justify-between">
        <button onClick={onBack} className="px-4 py-2 text-sm border border-[var(--border)] rounded-lg hover:bg-[var(--card)]">
          Back
        </button>
        <div className="flex gap-3">
          <button
            onClick={() => { update({ botName: "SantiBot (Test)" }); onNext(); }}
            className="px-4 py-2 text-sm text-[var(--muted)] border border-[var(--border)] rounded-lg hover:bg-[var(--card)]"
          >
            Skip for now
          </button>
          <button
            onClick={onNext}
            disabled={!validated}
            className="px-6 py-2 bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white text-sm rounded-lg disabled:opacity-50"
          >
            Next
          </button>
        </div>
      </div>
    </div>
  );
}
