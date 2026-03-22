"use client";

import { useState } from "react";
import type { EmbedField } from "@/lib/types";

export default function EmbedBuilderPage() {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [color, setColor] = useState("#0c95e9");
  const [author, setAuthor] = useState("");
  const [footer, setFooter] = useState("");
  const [imageUrl, setImageUrl] = useState("");
  const [thumbnailUrl, setThumbnailUrl] = useState("");
  const [fields, setFields] = useState<EmbedField[]>([]);

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
              <button className="flex-1 px-4 py-2 text-sm bg-[var(--accent)] text-white rounded-lg hover:bg-[var(--accent-hover)]">
                Save Template
              </button>
              <button className="flex-1 px-4 py-2 text-sm border border-[var(--border)] rounded-lg hover:bg-[var(--card-hover)]">
                Send to Channel
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
