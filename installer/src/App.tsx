import { useState } from "react";
import Welcome from "./pages/Welcome";
import TokenSetup from "./pages/TokenSetup";
import DeploymentMethod from "./pages/DeploymentMethod";
import Configuration from "./pages/Configuration";
import Dependencies from "./pages/Dependencies";
import Installing from "./pages/Installing";
import Complete from "./pages/Complete";
import StepIndicator from "./components/StepIndicator";
import MusicPlayer from "./components/MusicPlayer";

export type DeployMethod = "local" | "docker" | "vps";

export interface InstallerState {
  token: string;
  botName: string;
  deployMethod: DeployMethod;
  installPath: string;
  prefix: string;
  ownerId: string;
  enableDashboard: boolean;
}

const steps = [
  "Welcome",
  "Bot Token",
  "Deployment",
  "Configuration",
  "Dependencies",
  "Installing",
  "Complete",
];

export default function App() {
  const [step, setStep] = useState(0);
  const [state, setState] = useState<InstallerState>({
    token: "",
    botName: "",
    deployMethod: "local",
    installPath: "",
    prefix: ".",
    ownerId: "",
    enableDashboard: true,
  });

  const next = () => setStep((s) => Math.min(s + 1, steps.length - 1));
  const back = () => setStep((s) => Math.max(s - 1, 0));
  const update = (partial: Partial<InstallerState>) =>
    setState((s) => ({ ...s, ...partial }));

  return (
    <div className="h-screen flex flex-col bg-[var(--background)]">
      {/* Header */}
      <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--border)]">
        <h1 className="text-lg font-bold">
          <span className="text-[var(--accent)]">Santi</span>Bot Installer
        </h1>
        <span className="text-xs text-[var(--muted)]">v1.0.0</span>
      </div>

      {/* Step indicator */}
      {step > 0 && step < steps.length - 1 && (
        <StepIndicator steps={steps} current={step} />
      )}

      {/* Content */}
      <div className="flex-1 overflow-y-auto p-6">
        {step === 0 && <Welcome onNext={next} />}
        {step === 1 && <TokenSetup state={state} update={update} onNext={next} onBack={back} />}
        {step === 2 && <DeploymentMethod state={state} update={update} onNext={next} onBack={back} />}
        {step === 3 && <Configuration state={state} update={update} onNext={next} onBack={back} />}
        {step === 4 && <Dependencies onNext={next} onBack={back} />}
        {step === 5 && <Installing state={state} onNext={next} />}
        {step === 6 && <Complete state={state} />}
      </div>

      {/* Music Player — fixed at bottom */}
      <MusicPlayer />
    </div>
  );
}
