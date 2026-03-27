"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/api";
import ConfigPanel from "@/components/ConfigPanel";

interface Form {
  id: string;
  title: string;
  responseChannelId: string;
  questionsJson: string;
}

export default function FormsPage() {
  const { guildId } = useParams<{ guildId: string }>();
  const [forms, setForms] = useState<Form[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!guildId) return;

    apiFetch<Form[]>(`/api/guilds/${guildId}/config/forms`)
      .then((data) => {
        setForms(data);
        setLoading(false);
      })
      .catch((err) => {
        setError(err.message || "Failed to load forms");
        setLoading(false);
      });
  }, [guildId]);

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
          <p className="text-[var(--error)] font-medium mb-2">Failed to load forms</p>
          <p className="text-[var(--muted)] text-sm">{error}</p>
        </div>
      </div>
    );
  }

  const getQuestionCount = (questionsJson: string): number => {
    try {
      const parsed = JSON.parse(questionsJson);
      return Array.isArray(parsed) ? parsed.length : 0;
    } catch {
      return 0;
    }
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Forms</h1>
      <p className="text-[var(--muted)] mb-6">
        Create and manage custom forms for applications, reports, and feedback from your server members.
      </p>

      <div className="space-y-6">
        <ConfigPanel title="Managed Forms">
          {forms.length === 0 ? (
            <p className="text-[var(--muted)] text-sm">
              No forms created yet. Use bot commands to create them.
            </p>
          ) : (
            <div className="space-y-3">
              {forms.map((f) => (
                <div
                  key={f.id}
                  className="flex items-center justify-between p-3 rounded-lg bg-[var(--background)] border border-[var(--border)]"
                >
                  <div>
                    <p className="text-sm font-medium">{f.title}</p>
                    <p className="text-xs text-[var(--muted)]">
                      Response Channel: {f.responseChannelId} &middot;{" "}
                      {getQuestionCount(f.questionsJson)} question
                      {getQuestionCount(f.questionsJson) !== 1 ? "s" : ""}
                    </p>
                  </div>
                  <span className="text-xs px-2 py-1 rounded-full bg-green-500/20 text-green-400">
                    Active
                  </span>
                </div>
              ))}
            </div>
          )}
        </ConfigPanel>
      </div>
    </div>
  );
}
