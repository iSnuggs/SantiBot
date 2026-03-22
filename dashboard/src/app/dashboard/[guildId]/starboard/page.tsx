"use client";

import { useState } from "react";
import { useParams } from "next/navigation";
import ConfigPanel, { Toggle, InputField } from "@/components/ConfigPanel";

export default function StarboardPage() {
  const { guildId } = useParams();
  const [enabled, setEnabled] = useState(false);
  const [threshold, setThreshold] = useState("3");
  const [selfStar, setSelfStar] = useState(false);
  const [channelId, setChannelId] = useState("");
  const [hasChanges, setHasChanges] = useState(false);

  const handleChange = (setter: Function) => (value: any) => {
    setter(value);
    setHasChanges(true);
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Starboard</h1>
      <p className="text-[var(--muted)] mb-6">
        Highlight popular messages by collecting star reactions.
        When a message receives enough stars, it gets posted to the starboard channel.
      </p>

      <ConfigPanel
        title="Starboard Settings"
        hasChanges={hasChanges}
        onSave={() => setHasChanges(false)}
        onDiscard={() => setHasChanges(false)}
      >
        <Toggle
          label="Enable Starboard"
          checked={enabled}
          onChange={handleChange(setEnabled)}
        />
        <InputField
          label="Starboard Channel ID"
          value={channelId}
          onChange={handleChange(setChannelId)}
          placeholder="Enter channel ID"
        />
        <InputField
          label="Star Threshold"
          value={threshold}
          onChange={handleChange(setThreshold)}
          placeholder="3"
          type="number"
        />
        <Toggle
          label="Allow Self-Starring"
          checked={selfStar}
          onChange={handleChange(setSelfStar)}
        />
      </ConfigPanel>
    </div>
  );
}
