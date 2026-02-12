import { useEffect, useMemo, useState } from "react";
import {
  createBooking,
  getResources,
  login,
  seed,
  type Resource,
} from "./api";

// Konverterer "datetime-local" (lokal tid) til ISO string.
// Vi bruker Date(value).toISOString() som gir UTC.
// Det er helt OK for demo – backend jobber med DateTimeOffset.
function toIsoFromDateTimeLocal(value: string): string {
  // value eksempel: "2026-02-11T12:30"
  // new Date(value) tolker dette som lokal tid i nettleseren.
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) throw new Error("Ugyldig dato/tid.");
  return d.toISOString();
}

export default function App() {
  // Login
  const [email, setEmail] = useState("admin@demo.no");
  const [password, setPassword] = useState("admin");

  // UI state
  const [message, setMessage] = useState<string>("");
  const [error, setError] = useState<string>("");
  const [loading, setLoading] = useState(false);

  // Data
  const [resources, setResources] = useState<Resource[]>([]);

  // Booking form
  const [resourceId, setResourceId] = useState("");
  const [startLocal, setStartLocal] = useState("");
  const [endLocal, setEndLocal] = useState("");

  const isLoggedIn = useMemo(() => !!localStorage.getItem("token"), []);

  function setOk(msg: string) {
    setError("");
    setMessage(msg);
  }

  function setFail(msg: string) {
    setMessage("");
    setError(msg);
  }

  async function handleSeed() {
    setMessage("");
    setError("");
    setLoading(true);
    try {
      await seed();
      setOk("Seed ok ✅");
    } catch (e: any) {
      setFail(e?.message ?? "Seed feilet");
    } finally {
      setLoading(false);
    }
  }

  async function handleLogin() {
    setMessage("");
    setError("");
    setLoading(true);

    try {
      const res = await login({ email, password });
      localStorage.setItem("token", res.token);
      localStorage.setItem("email", res.email);
      localStorage.setItem("role", res.role);
      setOk(`Logget inn som ${res.email} (${res.role}) ✅`);
    } catch (e: any) {
      setFail(e?.message ?? "Innlogging feilet");
    } finally {
      setLoading(false);
      // Enkelt: refresh for å få "innlogget"-tilstand med én gang
      window.location.reload();
    }
  }

  async function handleLoadResources() {
    setMessage("");
    setError("");
    setLoading(true);

    try {
      const items = await getResources();
      setResources(items);

      // Hvis vi ikke har valgt ressurs enda, velg første automatisk.
      if (!resourceId && items.length > 0) setResourceId(items[0].id);

      setOk(`Hentet ${items.length} ressurser ✅`);
    } catch (e: any) {
      setFail(e?.message ?? "Kunne ikke hente ressurser");
    } finally {
      setLoading(false);
    }
  }

  function handleLogout() {
    localStorage.removeItem("token");
    localStorage.removeItem("email");
    localStorage.removeItem("role");
    window.location.reload();
  }

  async function handleCreateBooking() {
    setMessage("");
    setError("");
    setLoading(true);

    try {
      if (!resourceId) throw new Error("Velg en ressurs.");
      if (!startLocal || !endLocal) throw new Error("Velg start og slutt.");

      const startIso = toIsoFromDateTimeLocal(startLocal);
      const endIso = toIsoFromDateTimeLocal(endLocal);

      // En enkel validering i UI (backend validerer også)
      if (new Date(endIso) <= new Date(startIso)) {
        throw new Error("Slutt må være etter start.");
      }

      const created = await createBooking({
        resourceId,
        start: startIso,
        end: endIso,
      });

      setOk(`Booking opprettet ✅ (id: ${created.id})`);

      // Rydd skjema litt etter suksess
      setStartLocal("");
      setEndLocal("");
    } catch (e: any) {
      // Her får vi også fine meldinger for 409 overlapp
      setFail(e?.message ?? "Kunne ikke opprette booking");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    // Autoload ressurser hvis vi allerede er logget inn
    if (localStorage.getItem("token")) {
      handleLoadResources();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loggedInEmail = localStorage.getItem("email");
  const loggedInRole = localStorage.getItem("role");

  return (
    <div style={{ maxWidth: 950, margin: "40px auto", padding: 16, fontFamily: "system-ui" }}>
      <h1 style={{ marginBottom: 6 }}>Booking Platform</h1>
      <p style={{ marginTop: 0, opacity: 0.75 }}>
        Enkel UI for å teste innlogging, ressursliste og booking.
      </p>

      <div style={{ display: "flex", gap: 12, flexWrap: "wrap", alignItems: "end" }}>
        <button onClick={handleSeed} disabled={loading}>
          Seed demo-data
        </button>

        {!localStorage.getItem("token") ? (
          <>
            <div>
              <label style={{ display: "block", fontSize: 12 }}>E-post</label>
              <input value={email} onChange={(e) => setEmail(e.target.value)} />
            </div>

            <div>
              <label style={{ display: "block", fontSize: 12 }}>Passord</label>
              <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" />
            </div>

            <button onClick={handleLogin} disabled={loading}>
              Logg inn
            </button>
          </>
        ) : (
          <>
            <div style={{ padding: "6px 10px", border: "1px solid #ddd", borderRadius: 8 }}>
              <div style={{ fontSize: 12, opacity: 0.7 }}>Innlogget</div>
              <div style={{ fontWeight: 600 }}>{loggedInEmail ?? "ukjent"}</div>
              <div style={{ fontSize: 12, opacity: 0.7 }}>{loggedInRole ?? ""}</div>
            </div>

            <button onClick={handleLoadResources} disabled={loading}>
              Oppdater ressurser
            </button>

            <button onClick={handleLogout} disabled={loading}>
              Logg ut
            </button>
          </>
        )}
      </div>

      {(message || error) && (
        <div
          style={{
            marginTop: 16,
            padding: 12,
            borderRadius: 10,
            background: error ? "#3b0a0a" : "#0b2a12",
            color: "white",
          }}
        >
          {error ? `Feil: ${error}` : message}
        </div>
      )}

      <div style={{ marginTop: 28, display: "grid", gridTemplateColumns: "1fr 1fr", gap: 18 }}>
        <div style={{ padding: 14, border: "1px solid #ddd", borderRadius: 12 }}>
          <h2 style={{ marginTop: 0 }}>Ressurser</h2>

          {resources.length === 0 ? (
            <p style={{ opacity: 0.7 }}>Ingen ressurser lastet ennå.</p>
          ) : (
            <ul style={{ paddingLeft: 18, marginBottom: 0 }}>
              {resources.map((r) => (
                <li key={r.id} style={{ marginBottom: 10 }}>
                  <strong>{r.name}</strong> – {r.description}
                </li>
              ))}
            </ul>
          )}
        </div>

        <div style={{ padding: 14, border: "1px solid #ddd", borderRadius: 12 }}>
          <h2 style={{ marginTop: 0 }}>Ny booking</h2>

          {!isLoggedIn ? (
            <p style={{ opacity: 0.7 }}>Logg inn for å opprette booking.</p>
          ) : (
            <>
              <div style={{ marginBottom: 12 }}>
                <label style={{ display: "block", fontSize: 12 }}>Ressurs</label>
                <select
                  value={resourceId}
                  onChange={(e) => setResourceId(e.target.value)}
                  style={{ width: "100%" }}
                >
                  <option value="">Velg…</option>
                  {resources.map((r) => (
                    <option key={r.id} value={r.id}>
                      {r.name}
                    </option>
                  ))}
                </select>
              </div>

              <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
                <div>
                  <label style={{ display: "block", fontSize: 12 }}>Start</label>
                  <input
                    type="datetime-local"
                    value={startLocal}
                    onChange={(e) => setStartLocal(e.target.value)}
                    style={{ width: "100%" }}
                  />
                </div>

                <div>
                  <label style={{ display: "block", fontSize: 12 }}>Slutt</label>
                  <input
                    type="datetime-local"
                    value={endLocal}
                    onChange={(e) => setEndLocal(e.target.value)}
                    style={{ width: "100%" }}
                  />
                </div>
              </div>

              <button onClick={handleCreateBooking} disabled={loading} style={{ marginTop: 12 }}>
                Opprett booking
              </button>

              <p style={{ marginTop: 10, fontSize: 12, opacity: 0.75 }}>
                Tips: Hvis du får “overlapper”, betyr det at tidsrommet krasjer med en eksisterende booking.
              </p>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
