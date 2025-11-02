"use client";
import { useState } from "react";

export default function Dashboard() {
  const [resumeText, setResumeText] = useState("");
  const [jobDescription, setJobDescription] = useState("");
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<any>(null);

  async function analyze() {
    setLoading(true);
    setData(null);
    const res = await fetch("/api/analyze", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ resumeText, jobDescription })
    });
    const json = await res.json();
    setData(json);
    setLoading(false);
  }

  return (
    <div>
      <h1>CareerCoach Dashboard</h1>
      <p>Paste your CV and the job post to get an instant analysis.</p>
      <textarea value={resumeText} onChange={(e) => setResumeText(e.target.value)} placeholder="Paste your CV…" />
      <textarea value={jobDescription} onChange={(e) => setJobDescription(e.target.value)} placeholder="Paste job description…" />
      <button className="btn" onClick={analyze} disabled={loading}>
        {loading ? "Analyzing…" : "Analyze"}
      </button>

      {data && (
        <div className="card" style={{ marginTop: 20 }}>
          <h2>ATS Score: {data.atsScore}/100</h2>
          <h3>Key Gaps</h3>
          <ul>{data.keyGaps.map((g: string, i: number) => <li key={i}>{g}</li>)}</ul>
          <h3>Improved Bullets</h3>
          <ul>{data.improvedBullets.map((b: string, i: number) => <li key={i}>{b}</li>)}</ul>
          <h3>Interview Questions</h3>
          <ul>{data.interviewQuestions.map((q: string, i: number) => <li key={i}>{q}</li>)}</ul>
          <h3>Next Steps</h3>
          <ul>{data.nextSteps.map((n: string, i: number) => <li key={i}>{n}</li>)}</ul>
        </div>
      )}
    </div>
  );
}
