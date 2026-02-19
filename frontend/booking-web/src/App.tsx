import { useEffect, useMemo, useState } from "react";
import {
  cancelBooking,
  createBooking,
  getMyBookings,
  getResources,
  login,
  seed,
  type Booking,
  type Resource,
} from "./api";

/**
 * Konverterer "datetime-local" (lokal tid) til ISO string (UTC).
 * Dette gjør at backend får et entydig tidspunkt uansett tidssone.
 *
 * NB: Date(value).toISOString() tolker datetime-local som lokal tid.
 */
function toIsoFromDateTimeLocal(value: string): string {
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) throw new Error("Ugyldig dato/tid.");
  return d.toISOString();
}

/**
 * Formaterer ISO-dato til lesbar lokal dato/tid for UI.
 */
function formatLocal(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString();
}

export default function App() {
  // -------------------------
  // Login state
  // -------------------------
  const [email, setEmail] = useState("admin@demo.no");
  const [password, setPassword] = useState("admin");

  // -------------------------
  // UI state
  // -------------------------
  const [message, setMessage] = useState<string>("");
  const [error, setError] = useState<string>("");
  const [loading, setLoading] = useState(false);

  // -------------------------
  // Data state
  // -------------------------
  const [resources, setResources] = useState<Resource[]>([]);
  const [myBookings, setMyBookings] = useState<Booking[]>([]);

  // -------------------------
  // Booking form state
  // -------------------------
  const [resourceId, setResourceId] = useState("");
  const [startLocal, setStartLocal] = useState("");
  const [endLocal, setEndLocal] = useState("");

  /**
   * Bruker vi token i localStorage som "kilde til sannhet" for innlogging.
   * Dette er litt enkelt, men fungerer fint for en liten demo-app.
   */
  const token = localStorage.getItem("token");
  const isLoggedIn = !!token;

  /**
   * Lager et oppslag fra resourceId -> ressursnavn.
   * Dette gjør at vi kan vise ressursnavn i bookinglista (ikke bare UUID).
   */
  const resourceNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const r of resources) map.set(r.id, r.name);
    return map;
  }, [resources]);

  function setOk(msg: string) {
    setError("");
    setMessage(msg);
  }

  function setFail(msg: string) {
    setMessage("");
    setError(msg);
  }

  // ---------------------------------------
  // API actions
  // ---------------------------------------

  /**
   * Seeder demo-data på backend.
   * Vi holder denne som en egen knapp fordi dette er en dev-funksjon.
   */
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

  /**
   * Logger inn og lagrer token i localStorage.
   * Vi reloader siden etter login for å gjøre demoen enkel og robust.
   * (I en “ordentlig” app ville vi heller oppdatert state uten reload.)
   */
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
      window.location.reload();
    }
  }

  /**
   * Logger ut ved å fjerne token/metadata.
   * Reload gjør at vi nullstiller UI og state enkelt.
   */
  function handleLogout() {
    localStorage.removeItem("token");
    localStorage.removeItem("email");
    localStorage.removeItem("role");
    window.location.reload();
  }

  /**
   * Henter ressurser fra API.
   * Siden endpointet er beskyttet, må token være satt.
   */
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

  /**
   * Henter innlogget brukers bookinger.
   */
  async function handleLoadMyBookings() {
    setMessage("");
    setError("");
    setLoading(true);

    try {
      const items = await getMyBookings();
      setMyBookings(items);
      setOk(`Hentet ${items.length} bookinger ✅`);
    } catch (e: any) {
      setFail(e?.message ?? "Kunne ikke hente bookinger");
    } finally {
      setLoading(false);
    }
  }

  /**
   * Kansellerer en booking.
   * Backend er idempotent (hvis den allerede er kansellert, får du OK).
   */
  async function handleCancelBooking(id: string) {
    setMessage("");
    setError("");
    setLoading(true);

    try {
      await cancelBooking(id);
      setOk("Booking kansellert ✅");

      // Refresh liste så UI alltid viser korrekt status etter handling.
      const items = await getMyBookings();
      setMyBookings(items);
    } catch (e: any) {
      setFail(e?.message ?? "Kunne ikke kansellere booking");
    } finally {
      setLoading(false);
    }
  }

  /**
   * Oppretter booking med enkel validering i UI.
   * Backend validerer også (så UI-valideringen er bare “brukervennlighet”).
   */
  async function handleCreateBooking() {
    setMessage("");
    setError("");
    setLoading(true);

    try {
      if (!resourceId) throw new Error("Velg en ressurs.");
      if (!startLocal || !endLocal) throw new Error("Velg start og slutt.");

      const startIso = toIsoFromDateTimeLocal(startLocal);
      const endIso = toIsoFromDateTimeLocal(endLocal);

      if (new Date(endIso) <= new Date(startIso)) {
        throw new Error("Slutt må være etter start.");
      }

      const created = await createBooking({
        resourceId,
        start: startIso,
        end: endIso,
      });

      setOk(`Booking opprettet ✅ (id: ${created.id})`);

      // Rydd skjema etter suksess
      setStartLocal("");
      setEndLocal("");

      // Oppdater “mine bookinger” så du ser den nye bookingen med en gang.
      const items = await getMyBookings();
      setMyBookings(items);
    } catch (e: any) {
      // Her får vi også fine meldinger for 409 overlapp
      setFail(e?.message ?? "Kunne ikke opprette booking");
    } finally {
      setLoading(false);
    }
  }

  // ---------------------------------------
  // Autoload ved oppstart (hvis innlogget)
  // ---------------------------------------
  useEffect(() => {
    if (localStorage.getItem("token")) {
      // Vi henter både ressurser og mine bookinger i starten
      // slik at UI kommer opp “ferdig” med data.
      handleLoadResources();
      handleLoadMyBookings();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loggedInEmail = localStorage.getItem("email");
  const loggedInRole = localStorage.getItem("role");

  return (
    <div style={{ maxWidth: 980, margin: "40px auto", padding: 16, fontFamily: "system-ui" }}>
      <h1 style={{ marginBottom: 6 }}>Booking Platform</h1>
      <p style={{ marginTop: 0, opacity: 0.75 }}>
        Enkel UI for å teste innlogging, ressurser, booking og kansellering.
      </p>

      {/* Topplinje: seed + login/logout */}
      <div style={{ display: "flex", gap: 12, flexWrap: "wrap", alignItems: "end" }}>
        <button onClick={handleSeed} disabled={loading}>
          Seed demo-data
        </button>

        {!isLoggedIn ? (
          <>
            <div>
              <label style={{ display: "block", fontSize: 12 }}>E-post</label>
              <input value={email} onChange={(e) => setEmail(e.target.value)} />
            </div>

            <div>
              <label style={{ display: "block", fontSize: 12 }}>Passord</label>
              <input
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                type="password"
              />
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

            <button onClick={handleLoadMyBookings} disabled={loading}>
              Oppdater mine bookinger
            </button>

            <button onClick={handleLogout} disabled={loading}>
              Logg ut
            </button>
          </>
        )}
      </div>

      {/* Statusmelding/feil */}
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

      {/* Innhold */}
      <div
        style={{
          marginTop: 28,
          display: "grid",
          gridTemplateColumns: "1fr 1fr",
          gap: 18,
          alignItems: "start",
        }}
      >
        {/* Ressurser */}
        <div style={{ padding: 14, border: "1px solid #ddd", borderRadius: 12 }}>
          <h2 style={{ marginTop: 0 }}>Ressurser</h2>

          {!isLoggedIn ? (
            <p style={{ opacity: 0.7 }}>Logg inn for å hente ressurser.</p>
          ) : resources.length === 0 ? (
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

        {/* Ny booking */}
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

      {/* Mine bookinger */}
      <div style={{ padding: 14, border: "1px solid #ddd", borderRadius: 12, marginTop: 18 }}>
        <h2 style={{ marginTop: 0 }}>Mine bookinger</h2>

        {!isLoggedIn ? (
          <p style={{ opacity: 0.7 }}>Logg inn for å se dine bookinger.</p>
        ) : myBookings.length === 0 ? (
          <p style={{ opacity: 0.7 }}>Ingen bookinger ennå.</p>
        ) : (
          <div style={{ display: "grid", gap: 10 }}>
            {myBookings.map((b) => {
              const start = formatLocal(b.start);
              const end = formatLocal(b.end);

              const isCancelled = b.status === "Cancelled";
              const resourceName = resourceNameById.get(b.resourceId) ?? b.resourceId;

              return (
                <div
                  key={b.id}
                  style={{
                    padding: 12,
                    border: "1px solid #eee",
                    borderRadius: 10,
                    display: "flex",
                    justifyContent: "space-between",
                    gap: 12,
                    alignItems: "center",
                  }}
                >
                  <div>
                    <div style={{ fontWeight: 700 }}>
                      {start} → {end}
                    </div>

                    <div style={{ fontSize: 12, opacity: 0.8 }}>
                      Ressurs: <strong>{resourceName}</strong>
                    </div>

                    <div style={{ fontSize: 12, opacity: 0.8 }}>
                      Status:{" "}
                      <span style={{ fontWeight: 700 }}>
                        {b.status}
                      </span>
                    </div>

                    <div style={{ fontSize: 12, opacity: 0.6 }}>
                      ID: {b.id}
                    </div>
                  </div>

                  <div>
                    <button
                      onClick={() => handleCancelBooking(b.id)}
                      disabled={loading || isCancelled}
                      title={isCancelled ? "Allerede kansellert" : "Kanseller booking"}
                    >
                      Kanseller
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
