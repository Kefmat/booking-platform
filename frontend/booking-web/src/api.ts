const BASE_URL = import.meta.env.VITE_API_BASE_URL as string;

export type LoginRequest = { email: string; password: string };
export type LoginResponse = { token: string; role: string; email: string };

export type Resource = {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
};

export type BookingCreateRequest = {
  resourceId: string;
  start: string; // ISO string
  end: string;   // ISO string
};

export type BookingResponse = {
  id: string;
  resourceId: string;
  userId: string;
  start: string;
  end: string;
  status: string;
  createdAt: string;
};

function authHeader(): HeadersInit {
  const token = localStorage.getItem("token");
  if (!token) return {};
  return { Authorization: `Bearer ${token}` };
}

async function readErrorMessage(res: Response): Promise<string> {
  // Backend kan returnere tekst (BadRequest/Conflict), eller JSON, eller ingenting.
  const contentType = res.headers.get("content-type") ?? "";
  try {
    if (contentType.includes("application/json")) {
      const data = await res.json();
      return typeof data === "string" ? data : JSON.stringify(data);
    }
    const text = await res.text();
    return text || `HTTP ${res.status}`;
  } catch {
    return `HTTP ${res.status}`;
  }
}

export async function seed(): Promise<void> {
  const res = await fetch(`${BASE_URL}/dev/seed`, { method: "POST" });
  if (!res.ok) throw new Error(await readErrorMessage(res));
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
  const res = await fetch(`${BASE_URL}/resources`, { headers: authHeader() });

  if (res.status === 401) throw new Error("Ikke logget inn (401).");
  if (!res.ok) throw new Error(await readErrorMessage(res));

  return res.json();
}

export async function createBooking(req: BookingCreateRequest): Promise<BookingResponse> {
  const res = await fetch(`${BASE_URL}/bookings`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...authHeader(),
    },
    body: JSON.stringify(req),
  });

  if (res.status === 401) throw new Error("Du er ikke logget inn (401).");
  if (res.status === 409) throw new Error(await readErrorMessage(res)); // Overlapp
  if (!res.ok) throw new Error(await readErrorMessage(res));

  return res.json();
}

export type Booking = {
  id: string;
  resourceId: string;
  userId: string;
  start: string;
  end: string;
  status: string;
  createdAt?: string;
};

export async function getMyBookings(): Promise<Booking[]> {
  const res = await fetch(`${BASE_URL}/bookings/my`, {
    headers: { ...authHeader() },
  });

  if (res.status === 401) throw new Error("Ikke logget inn (401).");
  if (!res.ok) throw new Error(await readErrorMessage(res));
  return res.json();
}

export async function cancelBooking(id: string): Promise<void> {
  const res = await fetch(`${BASE_URL}/bookings/${id}/cancel`, {
    method: "POST",
    headers: { ...authHeader() },
  });

  if (res.status === 401) throw new Error("Ikke logget inn (401).");
  if (res.status === 403) throw new Error("Du har ikke tilgang til å kansellere denne bookingen (403).");
  if (res.status === 404) throw new Error("Fant ikke bookingen (404).");
  if (!res.ok) throw new Error(await readErrorMessage(res));
}