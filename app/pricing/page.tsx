export default function Pricing() {
  const plans = [
    { name: "Starter", price: "£0", note: "5 analyses/day", features: ["ATS score", "Bullet rewrites", "3 interview questions"] },
    { name: "Pro", price: "£14.99/mo", note: "Unlimited", features: ["Full CV rewrite", "LinkedIn export", "Advanced interview prep"] }
  ];

  return (
    <div>
      <h1>Pricing</h1>
      <div style={{ display: "grid", gap: 16 }}>
        {plans.map((p) => (
          <div key={p.name} className="card">
            <h2>{p.name}</h2>
            <p><b>{p.price}</b> • {p.note}</p>
            <ul>{p.features.map((f) => <li key={f}>{f}</li>)}</ul>
            <a className="btn" href="/dashboard">Get Started</a>
          </div>
        ))}
      </div>
    </div>
  );
}
