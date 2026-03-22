import { useState, useEffect } from "react";
import type { InstallerState } from "../App";

interface Props {
  state: InstallerState;
  onNext: () => void;
}

interface LogEntry {
  message: string;
  type: "info" | "success" | "error";
}

export default function Installing({ state, onNext }: Props) {
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [progress, setProgress] = useState(0);
  const [done, setDone] = useState(false);
  const [failed, setFailed] = useState(false);

  const addLog = (message: string, type: LogEntry["type"] = "info") => {
    setLogs((prev) => [...prev, { message, type }]);
  };

  useEffect(() => {
    const install = async () => {
      try {
        addLog(`Starting ${state.deployMethod} installation...`);
        setProgress(10);
        await delay(800);

        addLog("Downloading SantiBot...");
        setProgress(30);
        await delay(1200);

        addLog("Extracting files...");
        setProgress(50);
        await delay(800);

        addLog("Writing configuration...");
        setProgress(70);
        await delay(600);

        addLog(`Token: ${"*".repeat(20)}...${state.token.slice(-4) || "****"}`);
        addLog(`Owner ID: ${state.ownerId || "Not set"}`);
        addLog(`Prefix: ${state.prefix}`);
        setProgress(85);
        await delay(500);

        if (state.deployMethod === "docker") {
          addLog("Creating docker-compose.yml...");
          await delay(500);
          addLog("Pulling Docker image...");
          await delay(1000);
          addLog("Starting container...");
          await delay(800);
        } else {
          addLog("Building SantiBot...");
          await delay(1500);
        }

        setProgress(100);
        addLog("Installation complete!", "success");
        setDone(true);
      } catch (e: any) {
        addLog(`Error: ${e.message}`, "error");
        setFailed(true);
      }
    };

    install();
  }, []);

  return (
    <div className="max-w-lg mx-auto">
      <h2 className="text-2xl font-bold mb-2">Installing</h2>
      <p className="text-[var(--muted)] text-sm mb-6">
        {done ? "Installation complete!" : "Please wait while SantiBot is being installed..."}
      </p>

      {/* Progress bar */}
      <div className="w-full bg-[var(--border)] rounded-full h-2 mb-6">
        <div
          className={`h-2 rounded-full transition-all duration-500 ${
            failed ? "bg-[var(--error)]" : "bg-[var(--accent)]"
          }`}
          style={{ width: `${progress}%` }}
        />
      </div>

      {/* Log output */}
      <div className="bg-[#0a0b0f] border border-[var(--border)] rounded-xl p-4 mb-6 h-64 overflow-y-auto font-mono text-xs">
        {logs.map((log, i) => (
          <div
            key={i}
            className={`py-0.5 ${
              log.type === "success"
                ? "text-[var(--success)]"
                : log.type === "error"
                ? "text-[var(--error)]"
                : "text-[var(--muted)]"
            }`}
          >
            <span className="text-[var(--border)] mr-2">$</span>
            {log.message}
          </div>
        ))}
        {!done && !failed && (
          <div className="py-0.5 text-[var(--muted)]">
            <span className="animate-pulse">_</span>
          </div>
        )}
      </div>

      {done && (
        <button
          onClick={onNext}
          className="w-full px-6 py-3 bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white font-semibold rounded-lg"
        >
          Continue
        </button>
      )}

      {failed && (
        <button
          onClick={() => window.location.reload()}
          className="w-full px-6 py-3 bg-[var(--error)] hover:opacity-90 text-white font-semibold rounded-lg"
        >
          Retry
        </button>
      )}
    </div>
  );
}

function delay(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}
