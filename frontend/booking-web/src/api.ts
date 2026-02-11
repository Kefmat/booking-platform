const BASE_URL = import.meta.env.VITE_API_BASE_URL as string;

export type LoginRequest = { email: string; password: string };
export type LoginResponse = { token: string; role: string; email: string };

export type Resource = {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
};

// Returnerer alltid en type som fetch godtar som headers.
// Hvis vi ikke har token, returnerer vi et tomt objekt.
function authHeader(): HeadersInit {
  const token = localStorage.getItem("token");
  if (!token) return {};
  return { Authorization: `Bearer ${token}` };
}

export async function seed(): Promise<void> {
  const res = await fetch(`${BASE_URL}/dev/seed`, { method: "POST" });
  if (!res.ok) throw new Error("Seed feilet");
}

export async function login(req: LoginRequest): Promise<LoginResponse> {
  const res = await fetch(`${BASE_URL}/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(req),
  });

  if (!res.ok) throw new Error("Innlogging feilet (sjekk e-post/passord)");
  return res.json();
}

export async function getResources(): Promise<Resource[]> {
  const res = await fetch(`${BASE_URL}/resources`, {
    headers: authHeader(),
  });

  if (res.status === 401) throw new Error("Ikke logget inn (401).");
  if (!res.ok) throw new Error("Kunne ikke hente ressurser");

  return res.json();
}
