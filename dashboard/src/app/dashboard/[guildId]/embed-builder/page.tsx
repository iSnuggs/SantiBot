"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import type { EmbedField } from "@/lib/types";

interface SavedEmbed {
  id: number;
  name: string;
  embedJson: string;
  creatorId: string;
}

interface EmbedData {
  title?: string;
  description?: string;
  color?: string;
  author?: string;
  footer?: string;
  imageUrl?: string;
  thumbnailUrl?: string;
  fields?: EmbedField[];
}

export default function EmbedBuilderPage() {
  const { guildId } = useParams<{ guildId: string }>();
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [color, setColor] = useState("#0c95e9");
  const [author, setAuthor] = useState("");
  const [footer, setFooter] = useState("");
  const [imageUrl, setImageUrl] = useState("");
  const [thumbnailUrl, setThumbnailUrl] = useState("");
  const [fields, setFields] = useState<EmbedField[]>([]);

  const [savedEmbeds, setSavedEmbeds] = useState<SavedEmbed[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [deletingId, setDeletingId] = useState<number | null>(null);

  useEffect(() => {
    if (!guildId) return;

    apiFetch<SavedEmbed[]>(`/api/guilds/${guildId}/embeds/saved`)
      .then((data) => {
        setSavedEmbeds(data);
        setLoading(false);
      })
      .catch((err) => {
        setError(err.message || "Failed to load saved embeds");
        setLoading(false);
      });
  }, [guildId]);

  const addField = () => {
    setFields([...fields, { name: "", value: "", inline: false }]);
  };

  const updateField = (index: number, key: keyof EmbedField, value: any) => {
    const updated = [...fields];
    (updated[index] as any)[key] = value;
    setFields(updated);
  };

  const removeField = (index: number) => {
    setFields(fields.filter((_, i) => i !== index));
  };

  const handleSaveTemplate = async () => {
    const name = window.prompt("Enter a name for this template:");
    if (!name || !name.trim()) return;

    setSaving(true);
    try {
      const embed: EmbedData = {};
      if (title) embed.title = title;
      if (description) embed.description = description;
      if (color) embed.color = color;
      if (author) embed.author = author;
      if (footer) embed.footer = footer;
      if (imageUrl) embed.imageUrl = imageUrl;
      if (thumbnailUrl) embed.thumbnailUrl = thumbnailUrl;
      if (fields.length > 0) embed.fields = fields;

      const saved = await apiFetch<SavedEmbed>(`/api/guilds/${guildId}/embeds/saved`, {
        method: "POST",
        body: JSON.stringify({ name: name.trim(), embed }),
      });
      setSavedEmbeds([...savedEmbeds, saved]);
    } catch (err: any) {
      alert(err.message || "Failed to save template");
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteEmbed = async (embedId: number) => {
    if (!confirm("Are you sure you want to delete this template?")) return;

    setDeletingId(embedId);
    try {
      await apiFetch(`/api/guilds/${guildId}/embeds/saved/${embedId}`, {
        method: "DELETE",
      });
      setSavedEmbeds(savedEmbeds.filter((e) => e.id !== embedId));
    } catch (err: any) {
      alert(err.message || "Failed to delete template");
    } finally {
      setDeletingId(null);
    }
  };

  const handleLoadEmbed = (embed: SavedEmbed) => {
    try {
      const data: EmbedData = JSON.parse(embed.embedJson);
      setTitle(data.title || "");
      setDescription(data.description || "");
      setColor(data.color || "#0c95e9");
      setAuthor(data.author || "");
      setFooter(data.footer || "");
      setImageUrl(data.imageUrl || "");
      setThumbnailUrl(data.thumbnailUrl || "");
      setFields(data.fields || []);
    } catch {
      alert("Failed to parse embed data");
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="w-8 h-8 border-2 border-[var(--accent)] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="text-center">
          <p className="text-[var(--error)] font-medium mb-2">Failed to load embed builder</p>
          <p className="text-[var(--muted)] text-sm">{error}</p>
        </div>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Embed Builder</h1>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Editor */}
        <div className="space-y-4">
          <div className="bg-[var(--card)] border border-[var(--border)] rounded-xl p-6">
            <h2 className="font-semibold mb-4">Editor</h2>

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium mb-1.5">Author</label>
                <input
                  value={author}
                  onChange={(e) => setAuthor(e.target.value)}
                  placeholder="Author name"
                  className="w-full px-3 py-2 bg-[var(--background)] border border-[var(--border)] rounded-lg text-sm focus:border-[var(--accent)] focus:outline-none"
                />
              </div>

              <div>
                <label className="block text-sm font-medium mb-1.5">Title</label>
                <input
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder="Embed title"
                  className="w-full px-3 py-2 bg-[var(--background)] border border-[var(--border)] rounded-lg text-sm focus:border-[var(--accent)] focus:outline-none"
                />
              </div>

              <div>
                <label className="block text-sm font-medium mb-1.5">Description</label>
                <textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Embed description (supports markdown)"
                  rows={4}
                  className="w-full px-3 py-2 bg-[var(--background)] border border-[var(--border)] rounded-lg text-sm focus:border-[var(--accent)] focus:outline-none resize-y"
                />
              </div>

              <div className="flex gap-4">
                <div className="flex-1">
                  <label className="block text-sm font-medium mb-1.5">Color</label>
                  <div className="flex gap-2">
                    <input
                      type="color"
                      value={color}
                      onChange={(e) => setColor(e.target.value)}
                      className="w-10 h-10 rounded border border-[var(--border)] cursor-pointer"
                    />
                    <input
                      value={color}
                      onChange={(e) => setColor(e.target.value)}
                      className="flex-1 px-3 py-2 bg-[var(--background)] border border-[var(--border)] rounded-lg text-sm focus:border-[var(--accent)] focus:outline-none"
                    />
                  </div>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium mb-1.5">Image URL</label>
                <input
                  value={imageUrl}
                  onChange={(e) => setImageUrl(e.target.value)}
                  placeholder="https://example.com/image.png"
                  className="w-full px-3 py-2 bg-[var(--background)] border border-[var(--border)] rounded-lg text-sm focus:border-[var(--accent)] focus:outline-none"
                />
              </div>

              <div>
                <label className="block text-sm font-medium mb-1.5">Thumbnail URL</label>
                <input
                  value={thumbnailUrl}
                  onChange={(e) => setThumbnailUrl(e.target.value)}
                  placeholder="https://example.com/thumb.png"
                  className="w-full px-3 py-2 bg-[var(--background)] border border-[var(--border)] rounded-lg text-sm focus:border-[var(--accent)] focus:outline-none"
                />
              </div>

              <div>
                <label className="block text-sm font-medium mb-1.5">Footer</label>
                <input
                  value={footer}
                  onChange={(e) => setFooter(e.target.value)}
                  placeholder="Footer text"
                  className="w-full px-3 py-2 bg-[var(--background)] border border-[var(--border)] rounded-lg text-sm focus:border-[var(--accent)] focus:outline-none"
                />
              </div>
            </div>

            {/* Fields */}
            <div className="mt-6">
              <div className="flex items-center justify-between mb-3">
                <h3 className="font-medium text-sm">Fields</h3>
                <button
                  onClick={addField}
                  className="px-3 py-1 text-xs bg-[var(--accent)] text-white rounded-lg hover:bg-[var(--accent-hover)]"
                >
                  + Add Field
                </button>
              </div>

              {fields.map((field, i) => (
                <div
                  key={i}
                  className="bg-[var(--background)] border border-[var(--border)] rounded-lg p-3 mb-2"
                >
                  <div className="flex gap-2 mb-2">
                    <input
                      value={field.name}
                      onChange={(e) => updateField(i, "name", e.target.value)}
                      placeholder="Field name"
                      className="flex-1 px-2 py-1 bg-[var(--card)] border border-[var(--border)] rounded text-sm focus:outline-none"
                    />
                    <button
                      onClick={() => removeField(i)}
                      className="px-2 text-[var(--error)] hover:bg-[var(--card)] rounded"
                    >
                      X
                    </button>
                  </div>
                  <textarea
                    value={field.value}
                    onChange={(e) => updateField(i, "value", e.target.value)}
                    placeholder="Field value"
                    rows={2}
                    className="w-full px-2 py-1 bg-[var(--card)] border border-[var(--border)] rounded text-sm focus:outline-none resize-y mb-2"
                  />
                  <label className="flex items-center gap-2 text-xs text-[var(--muted)]">
                    <input
                      type="checkbox"
                      checked={field.inline}
                      onChange={(e) => updateField(i, "inline", e.target.checked)}
                      className="rounded"
                    />
                    Inline
                  </label>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Preview */}
        <div>
          <div className="bg-[var(--card)] border border-[var(--border)] rounded-xl p-6 sticky top-8">
            <h2 className="font-semibold mb-4">Preview</h2>

            <div className="bg-[#2f3136] rounded-lg overflow-hidden">
              <div
                className="border-l-4 p-4"
                style={{ borderColor: color }}
              >
                {author && (
                  <div className="text-xs text-gray-300 font-medium mb-1">
                    {author}
                  </div>
                )}
                {title && (
                  <div className="text-[#00b0f4] font-semibold mb-2">
                    {title}
                  </div>
                )}
                {description && (
                  <div className="text-sm text-gray-300 mb-3 whitespace-pre-wrap">
                    {description}
                  </div>
                )}
                {fields.length > 0 && (
                  <div className="grid grid-cols-1 gap-2 mb-3">
                    <div className="flex flex-wrap gap-x-4 gap-y-2">
                      {fields.map((field, i) => (
                        <div
                          key={i}
                          className={field.inline ? "min-w-[120px] flex-1" : "w-full"}
                        >
                          <div className="text-xs font-semibold text-gray-300">
                            {field.name || "Field name"}
                          </div>
                          <div className="text-sm text-gray-400">
                            {field.value || "Field value"}
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
                {imageUrl && (
                  <div className="mb-3">
                    <div className="text-xs text-gray-500 bg-[#36393f] rounded p-2">
                      [Image: {imageUrl}]
                    </div>
                  </div>
                )}
                {footer && (
                  <div className="text-xs text-gray-500 mt-2">{footer}</div>
                )}
              </div>
            </div>

            <div className="flex gap-3 mt-4">
              <button
                onClick={handleSaveTemplate}
                disabled={saving}
                className="flex-1 px-4 py-2 text-sm bg-[var(--accent)] text-white rounded-lg hover:bg-[var(--accent-hover)] disabled:opacity-50"
              >
                {saving ? "Saving..." : "Save Template"}
              </button>
              <button
                onClick={() => alert("Send to Channel requires a channel ID. This feature will be available in a future update.")}
                className="flex-1 px-4 py-2 text-sm border border-[var(--border)] rounded-lg hover:bg-[var(--card-hover)]"
                title="Coming soon"
              >
                Send to Channel
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Saved Templates */}
      <div className="mt-8">
        <div className="bg-[var(--card)] border border-[var(--border)] rounded-xl p-6">
          <h2 className="font-semibold mb-4">Saved Templates</h2>

          {savedEmbeds.length === 0 ? (
            <p className="text-[var(--muted)] text-sm">
              No saved templates yet. Build an embed above and click &quot;Save Template&quot; to save it.
            </p>
          ) : (
            <div className="space-y-3">
              {savedEmbeds.map((embed) => {
                let parsed: EmbedData = {};
                try {
                  parsed = JSON.parse(embed.embedJson);
                } catch {
                  // ignore parse errors
                }

                return (
                  <div
                    key={embed.id}
                    className="flex items-center justify-between p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]"
                  >
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium">{embed.name}</p>
                      <p className="text-xs text-[var(--muted)] truncate">
                        {parsed.title || "No title"} &middot;{" "}
                        {parsed.fields?.length || 0} field{(parsed.fields?.length || 0) !== 1 ? "s" : ""}
                      </p>
                    </div>
                    <div className="flex gap-2 ml-3">
                      <button
                        onClick={() => handleLoadEmbed(embed)}
                        className="px-3 py-1 text-xs bg-[var(--accent)] text-white rounded-lg hover:bg-[var(--accent-hover)]"
                      >
                        Load
                      </button>
                      <button
                        onClick={() => handleDeleteEmbed(embed.id)}
                        disabled={deletingId === embed.id}
                        className="px-3 py-1 text-xs bg-red-500/20 text-red-400 rounded-lg hover:bg-red-500/30 disabled:opacity-50"
                      >
                        {deletingId === embed.id ? "..." : "Delete"}
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
