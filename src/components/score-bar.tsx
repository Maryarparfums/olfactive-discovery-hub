interface Props {
  label: string;
  value: number; // 0-10
  caption?: string;
}

export function ScoreBar({ label, value, caption }: Props) {
  const pct = Math.max(0, Math.min(10, value)) * 10;
  return (
    <div className="space-y-2">
      <div className="flex items-baseline justify-between">
        <span className="text-[10px] uppercase tracking-widest text-muted-foreground">
          {label}
        </span>
        <span className="text-[11px] font-mono text-foreground">
          {caption ?? `${value}/10`}
        </span>
      </div>
      <div className="h-px bg-foreground/10 w-full overflow-hidden">
        <div
          className="h-full bg-foreground/60"
          style={{ width: `${pct}%` }}
        />
      </div>
    </div>
  );
}
