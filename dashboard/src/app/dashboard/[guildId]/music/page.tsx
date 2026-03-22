"use client";

import { useState } from "react";
import ConfigPanel, { Toggle, InputField, SelectField } from "@/components/ConfigPanel";

export default function MusicPage() {
  const [enabled, setEnabled] = useState(true);
  const [djOnly, setDjOnly] = useState(false);
  const [autoDisconnect, setAutoDisconnect] = useState(true);
  const [announceTrack, setAnnounceTrack] = useState(true);
  const [voteSkip, setVoteSkip] = useState(true);
  const [defaultVolume, setDefaultVolume] = useState("50");
  const [maxQueueSize, setMaxQueueSize] = useState("100");
  const [disconnectTimeout, setDisconnectTimeout] = useState("300");
  const [defaultSource, setDefaultSource] = useState("youtube");
  const [hasChanges, setHasChanges] = useState(false);

  const handleChange = (setter: Function) => (value: any) => {
    setter(value);
    setHasChanges(true);
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Music</h1>
      <p className="text-[var(--muted)] mb-6">
        Configure music playback settings, DJ restrictions, and queue behavior for your server.
      </p>

      <div className="space-y-6">
        <ConfigPanel
          title="Playback Settings"
          hasChanges={hasChanges}
          onSave={() => setHasChanges(false)}
          onDiscard={() => setHasChanges(false)}
        >
          <Toggle label="Enable Music" checked={enabled} onChange={handleChange(setEnabled)} />
          <Toggle label="DJ Role Required" checked={djOnly} onChange={handleChange(setDjOnly)} />
          <Toggle label="Announce Now Playing" checked={announceTrack} onChange={handleChange(setAnnounceTrack)} />
          <Toggle label="Vote Skip" checked={voteSkip} onChange={handleChange(setVoteSkip)} />
          <SelectField
            label="Default Source"
            value={defaultSource}
            onChange={handleChange(setDefaultSource)}
            options={[
              { value: "youtube", label: "YouTube" },
              { value: "soundcloud", label: "SoundCloud" },
              { value: "spotify", label: "Spotify" },
            ]}
          />
        </ConfigPanel>

        <ConfigPanel title="Queue & Connection">
          <InputField
            label="Default Volume (%)"
            value={defaultVolume}
            onChange={handleChange(setDefaultVolume)}
            placeholder="50"
            type="number"
          />
          <InputField
            label="Max Queue Size"
            value={maxQueueSize}
            onChange={handleChange(setMaxQueueSize)}
            placeholder="100"
            type="number"
          />
          <Toggle label="Auto-Disconnect When Empty" checked={autoDisconnect} onChange={handleChange(setAutoDisconnect)} />
          <InputField
            label="Disconnect Timeout (seconds)"
            value={disconnectTimeout}
            onChange={handleChange(setDisconnectTimeout)}
            placeholder="300"
            type="number"
          />
        </ConfigPanel>
      </div>
    </div>
  );
}
