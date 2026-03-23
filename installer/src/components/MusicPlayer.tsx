import { useState, useRef, useEffect } from "react";

const tracks = [
  { name: "Vintage", file: "/music/vintage.mp3", artist: "HoliznaCC0" },
  { name: "Glad To Be Stuck Inside", file: "/music/glad-to-be-stuck-inside.mp3", artist: "HoliznaCC0" },
  { name: "Creature Comforts", file: "/music/creature-comforts.mp3", artist: "HoliznaCC0" },
];

export default function MusicPlayer() {
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const [playing, setPlaying] = useState(false);
  const [currentTrack, setCurrentTrack] = useState(0);
  const [volume, setVolume] = useState(0.3);
  const [muted, setMuted] = useState(false);

  useEffect(() => {
    if (audioRef.current) {
      audioRef.current.volume = volume;
    }
  }, [volume]);

  useEffect(() => {
    if (audioRef.current && playing) {
      audioRef.current.src = tracks[currentTrack].file;
      audioRef.current.play().catch(() => {});
    }
  }, [currentTrack]);

  const toggle = () => {
    if (!audioRef.current) return;
    if (playing) {
      audioRef.current.pause();
    } else {
      audioRef.current.src = tracks[currentTrack].file;
      audioRef.current.play().catch(() => {});
    }
    setPlaying(!playing);
  };

  const next = () => {
    setCurrentTrack((c) => (c + 1) % tracks.length);
  };

  const prev = () => {
    setCurrentTrack((c) => (c - 1 + tracks.length) % tracks.length);
  };

  const handleEnded = () => {
    next();
  };

  const toggleMute = () => {
    if (audioRef.current) {
      audioRef.current.muted = !muted;
      setMuted(!muted);
    }
  };

  return (
    <div className="flex items-center gap-2 px-3 py-2 bg-[var(--card)] border-t border-[var(--border)]">
      <audio ref={audioRef} onEnded={handleEnded} />

      {/* Controls */}
      <button
        onClick={prev}
        className="w-7 h-7 flex items-center justify-center text-xs text-[var(--muted)] hover:text-[var(--foreground)] transition-colors"
        title="Previous"
      >
        ⏮
      </button>

      <button
        onClick={toggle}
        className="w-8 h-8 flex items-center justify-center rounded-full bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white text-sm transition-colors"
        title={playing ? "Pause" : "Play"}
      >
        {playing ? "⏸" : "▶"}
      </button>

      <button
        onClick={next}
        className="w-7 h-7 flex items-center justify-center text-xs text-[var(--muted)] hover:text-[var(--foreground)] transition-colors"
        title="Next"
      >
        ⏭
      </button>

      {/* Track info */}
      <div className="flex-1 min-w-0 mx-2">
        <div className="text-xs font-medium truncate">
          {tracks[currentTrack].name}
        </div>
        <div className="text-[10px] text-[var(--muted)] truncate">
          {tracks[currentTrack].artist} — CC0
        </div>
      </div>

      {/* Volume */}
      <button
        onClick={toggleMute}
        className="text-xs text-[var(--muted)] hover:text-[var(--foreground)]"
        title={muted ? "Unmute" : "Mute"}
      >
        {muted ? "🔇" : volume > 0.5 ? "🔊" : "🔉"}
      </button>

      <input
        type="range"
        min="0"
        max="1"
        step="0.05"
        value={muted ? 0 : volume}
        onChange={(e) => {
          setVolume(parseFloat(e.target.value));
          setMuted(false);
        }}
        className="w-16 h-1 accent-[var(--accent)]"
        title="Volume"
      />
    </div>
  );
}
