import { NextRequest, NextResponse } from "next/server";

export async function POST(req: NextRequest) {
  const { resumeText, jobDescription } = await req.json();
  if (!resumeText || !jobDescription)
    return NextResponse.json({ error: "Missing data" }, { status: 400 });

  const cvWords = resumeText.toLowerCase().match(/\b[a-z]{3,}\b/g) || [];
  const jdWords = jobDescription.toLowerCase().match(/\b[a-z]{3,}\b/g) || [];
  const overlap = jdWords.filter((w) => cvWords.includes(w));
  const atsScore = Math.min(100, Math.round((overlap.length / jdWords.length) * 100));

  const keyGaps = jdWords.filter((w) => !cvWords.includes(w)).slice(0, 10);
  const improvedBullets = overlap.slice(0, 5).map(
    (w) => `Show measurable impact using "${w}" — e.g., Delivered ${w} initiatives improving efficiency by 20%.`
  );
  const interviewQuestions = [
    "Tell me about a project where you demonstrated leadership.",
    "What was your biggest measurable impact?",
    "How do you handle ambiguity?",
    "Describe a time you improved a process.",
    "What are your long-term goals?"
  ];
  const nextSteps = [
    "Include 2–3 quantified metrics per role.",
    "Tailor keywords to match job description phrasing.",
    "Use active verbs: Delivered, Implemented, Improved.",
    "Focus on measurable outcomes.",
    "Keep summary under 80 words with clear skills."
  ];

  return NextResponse.json({ atsScore, keyGaps, improvedBullets, interviewQuestions, nextSteps });
}
