import { useState, useEffect } from "react";

interface Dep {
  name: string;
  label: string;
  icon: string;
  installed: boolean;
  version?: string;
}

interface Props {
  onNext: () => void;
  onBack: () => void;
}

export default function Dependencies({ onNext, onBack }: Props) {
  const [deps, setDeps] = useState<Dep[]>([
    { name: "dotnet", label: ".NET 8 SDK", icon: "🟣", installed: false },
    { name: "ffmpeg", label: "FFmpeg (Music)", icon: "🎵", installed: false },
    { name: "ytdlp", label: "yt-dlp (Music)", icon: "📥", installed: false },
  ]);
  const [checking, setChecking] = useState(true);

  useEffect(() => {
    // Simulate dependency check (in real Tauri app, calls check_dependencies command)
    const timer = setTimeout(() => {
      setDeps((prev) =>
        prev.map((d) => ({
          ...d,
          installed: false, // Will be set by Tauri command in production
        }))
      );
      setChecking(false);
    }, 1500);
    return () => clearTimeout(timer);
  }, []);

  const allInstalled = deps.every((d) => d.installed);

  return (
    <div className="max-w-lg mx-auto">
      <h2 className="text-2xl font-bold mb-2">Dependencies</h2>
      <p className="text-[var(--muted)] text-sm mb-6">
        SantiBot requires the following software. We'll check what's installed.
      </p>

      <div className="bg-[var(--card)] border border-[var(--border)] rounded-xl p-6 mb-6">
        {checking ? (
          <div className="flex items-center gap-3 py-4">
            <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-[var(--accent)]" />
            <span className="text-sm">Checking dependencies...</span>
          </div>
        ) : (
          <div className="space-y-3">
            {deps.map((dep) => (
              <div
                key={dep.name}
                className="flex items-center justify-between p-3 rounded-lg bg-[var(--background)]"
              >
                <div className="flex items-center gap-3">
                  <span className="text-xl">{dep.icon}</span>
                  <div>
                    <div className="text-sm font-medium">{dep.label}</div>
                    {dep.version && (
                      <div className="text-xs text-[var(--muted)]">v{dep.version}</div>
                    )}
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  {dep.installed ? (
                    <span className="text-[var(--success)] text-sm font-medium">Installed</span>
                  ) : (
                    <button className="px-3 py-1 text-xs bg-[var(--accent)] text-white rounded-lg hover:bg-[var(--accent-hover)]">
                      Install
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="bg-[var(--card)] border border-[var(--border)] rounded-xl p-4 mb-6">
        <p className="text-xs text-[var(--muted)]">
          FFmpeg and yt-dlp are only required for music features. You can skip them if you
          don't plan to use music commands.
        </p>
      </div>

      <div className="flex justify-between">
        <button onClick={onBack} className="px-4 py-2 text-sm border border-[var(--border)] rounded-lg hover:bg-[var(--card)]">
          Back
        </button>
        <button onClick={onNext} className="px-6 py-2 bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white text-sm rounded-lg">
          {allInstalled ? "Next" : "Skip & Continue"}
        </button>
      </div>
    </div>
  );
}
