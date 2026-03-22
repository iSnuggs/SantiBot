interface Props {
  onNext: () => void;
}

export default function Welcome({ onNext }: Props) {
  return (
    <div className="flex flex-col items-center justify-center h-full text-center">
      <div className="mb-8">
        <div className="text-6xl mb-4">🐾</div>
        <h1 className="text-4xl font-bold mb-2">
          <span className="text-[var(--accent)]">Santi</span>Bot
        </h1>
        <p className="text-[var(--muted)] text-lg">
          The ultimate open-source Discord bot
        </p>
      </div>

      <div className="max-w-md text-sm text-[var(--muted)] mb-8 space-y-2">
        <p>This installer will guide you through setting up SantiBot on your server.</p>
        <p>You'll need a Discord Bot Token to get started.</p>
      </div>

      <button
        onClick={onNext}
        className="px-8 py-3 bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white font-semibold rounded-lg transition-colors"
      >
        Get Started
      </button>

      <p className="mt-12 text-xs text-[var(--muted)]">
        Named after Santi, a beloved companion
      </p>
    </div>
  );
}
