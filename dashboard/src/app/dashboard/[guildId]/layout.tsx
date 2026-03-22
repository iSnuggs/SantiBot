"use client";

import Sidebar from "@/components/Sidebar";
import { useParams } from "next/navigation";

export default function GuildLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const params = useParams();
  const guildId = params.guildId as string;

  return (
    <div className="flex min-h-screen">
      <Sidebar guildId={guildId} />
      <main className="flex-1 lg:ml-0 p-6 lg:p-8 pt-16 lg:pt-8">
        {children}
      </main>
    </div>
  );
}
