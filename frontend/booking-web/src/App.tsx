import { useEffect, useState } from "react";
import { getResources, login, seed, type Resource } from "./api";

export default function App() {
  const [email, setEmail] = useState("admin@demo.no");
  const [password, setPassword] = useState("admin");
  const [message, setMessage] = useState<string>("");
  const [resources, setResources] = useState<Resource[]>([]);
  const [loading, setLoading] = useState(false);

  const isLoggedIn = !!localStorage.getItem("token");

  async function handleSeed() {
    setMessage("");
    setLoading(true);
    try {
      await seed();
      setMessage("Seed ok ✅");
    } catch (e: any) {
      setMessage(e?.message ?? "Seed feilet");
    } finally {
      setLoading(false);
    }
  }

  async function handleLogin() {
    setMessage("");
    setLoading(true);
    try {
      const res = await login({ email, password });
      localStorage.setItem("token", res.token);
      localStorage.setItem("email", res.email);
      localStorage.setItem("role", res.role);
      setMessage(`Logget inn som ${res.email} (${res.role}) ✅`);
    } catch (e: any) {
      setMessage(e?.message ?? "Innlogging feilet");
    } finally {
      setLoading(false);
      // Enkelt: refresh så vi får “logged in”-state + auto-load
      window.location.reload();
    }
  }

  async function handleLoadResources() {
    setMessage("");
    setLoading(true);
    try {
      const items = await getResources();
      setResources(items);
      setMessage(`Hentet ${items.length} ressurser ✅`);
    } catch (e: any) {
      setMessage(e?.message ?? "Kunne ikke hente ressurser");
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

  useEffect(() => {
    // Auto-hent ressurser hvis vi allerede har token
    if (localStorage.getItem("token")) {
      handleLoadResources();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div style={{ maxWidth: 900, margin: "40px auto", padding: 16, fontFamily: "system-ui" }}>
      <h1>Booking Platform</h1>
      <p style={{ marginTop: 0, opacity: 0.75 }}>
        Enkel frontend for å teste innlogging og ressursliste.
      </p>

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
              <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" />
            </div>

            <button onClick={handleLogin} disabled={loading}>
              Logg inn
            </button>
          </>
        ) : (
          <>
            <button onClick={handleLoadResources} disabled={loading}>
              Hent ressurser
            </button>

            <button onClick={handleLogout} disabled={loading}>
              Logg ut
            </button>
          </>
        )}
      </div>

      {message && (
        <div style={{ marginTop: 16, padding: 12, background: "#111", color: "white", borderRadius: 8 }}>
          {message}
        </div>
      )}

      <h2 style={{ marginTop: 28 }}>Ressurser</h2>

      {resources.length === 0 ? (
        <p style={{ opacity: 0.7 }}>Ingen ressurser lastet ennå.</p>
      ) : (
        <ul style={{ paddingLeft: 18 }}>
          {resources.map((r) => (
            <li key={r.id} style={{ marginBottom: 10 }}>
              <strong>{r.name}</strong> – {r.description}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
