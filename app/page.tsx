export default function Home() {
  return (
    <div className="card">
      <h1>Get hired faster with an AI Career Coach</h1>
      <p>Paste your CV and a job post — get instant ATS score, bullet rewrites, and interview prep.</p>
      <div style={{ marginTop: 16 }}>
        <a className="btn" href="/dashboard">Start Free</a>
        <a className="btn secondary" href="/pricing" style={{ marginLeft: 10 }}>See Pricing</a>
      </div>
    </div>
  );
}
