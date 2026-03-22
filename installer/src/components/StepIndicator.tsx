interface Props {
  steps: string[];
  current: number;
}

export default function StepIndicator({ steps, current }: Props) {
  return (
    <div className="flex items-center gap-1 px-6 py-3 border-b border-[var(--border)] overflow-x-auto">
      {steps.map((step, i) => (
        <div key={step} className="flex items-center">
          <div
            className={`flex items-center gap-2 px-2 py-1 rounded-full text-xs whitespace-nowrap ${
              i === current
                ? "bg-[var(--accent)] text-white"
                : i < current
                ? "text-[var(--accent)]"
                : "text-[var(--muted)]"
            }`}
          >
            <span
              className={`w-5 h-5 rounded-full flex items-center justify-center text-[10px] font-bold ${
                i < current
                  ? "bg-[var(--accent)] text-white"
                  : i === current
                  ? "bg-white text-[var(--accent)]"
                  : "bg-[var(--border)] text-[var(--muted)]"
              }`}
            >
              {i < current ? "✓" : i + 1}
            </span>
            <span className="hidden sm:inline">{step}</span>
          </div>
          {i < steps.length - 1 && (
            <div
              className={`w-6 h-px mx-1 ${
                i < current ? "bg-[var(--accent)]" : "bg-[var(--border)]"
              }`}
            />
          )}
        </div>
      ))}
    </div>
  );
}
