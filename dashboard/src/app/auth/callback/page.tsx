"use client";

import { Suspense, useEffect } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { setToken } from "@/lib/api";

function CallbackHandler() {
  const router = useRouter();
  const searchParams = useSearchParams();

  useEffect(() => {
    const token = searchParams.get("token");
    if (token) {
      setToken(token);
      router.push("/dashboard");
    } else {
      router.push("/");
    }
  }, [searchParams, router]);

  return (
    <div className="text-center">
      <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-[var(--accent)] mx-auto mb-4" />
      <p className="text-[var(--muted)]">Logging in...</p>
    </div>
  );
}

export default function AuthCallback() {
  return (
    <main className="flex min-h-screen items-center justify-center">
      <Suspense fallback={<div className="animate-spin rounded-full h-12 w-12 border-b-2 border-[var(--accent)]" />}>
        <CallbackHandler />
      </Suspense>
    </main>
  );
}
