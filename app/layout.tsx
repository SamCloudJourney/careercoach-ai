import "./globals.css";

export const metadata = {
  title: "CareerCoach.ai",
  description: "AI that fixes your CV, interview answers, and LinkedIn — instantly."
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <div className="container">
          <header style={{ display: "flex", alignItems: "center", gap: "16px", marginBottom: "24px" }}>
            <a href="/" style={{ fontWeight: 700, fontSize: "18px" }}>CareerCoach.ai</a>
            <a href="/dashboard">Dashboard</a>
            <a href="/pricing">Pricing</a>
            <div style={{ marginLeft: "auto" }}>
              <a className="btn secondary" href="/dashboard">Try Free</a>
            </div>
          </header>
          {children}
          <footer style={{ marginTop: 48, fontSize: 12, color: "#9aa7b2" }}>
            © {new Date().getFullYear()} CareerCoach.ai — Not affiliated with employers or ATS providers.
          </footer>
        </div>
      </body>
    </html>
  );
}
