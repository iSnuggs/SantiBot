"use client";

import { useEffect, useState } from "react";
import { apiFetch, getToken, clearToken } from "@/lib/api";
import type { User } from "@/lib/types";

export function useAuth() {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = getToken();
    if (!token) {
      setLoading(false);
      return;
    }

    apiFetch<User>("/api/auth/me")
      .then(setUser)
      .catch(() => {
        clearToken();
        setUser(null);
      })
      .finally(() => setLoading(false));
  }, []);

  const logout = () => {
    clearToken();
    setUser(null);
    window.location.href = "/";
  };

  return { user, loading, logout, isAuthenticated: !!user };
}
