"use client";

import { motion } from "framer-motion";
import Link from "next/link";

export default function Home() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-slate-900 to-black text-white flex flex-col items-center justify-center px-6 py-16">
      <motion.div
        initial={{ opacity: 0, y: 30 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.8 }}
        className="text-center max-w-3xl"
      >
        <h1 className="text-5xl sm:text-6xl font-extrabold tracking-tight bg-clip-text text-transparent bg-gradient-to-r from-cyan-400 via-blue-500 to-purple-500">
          CareerCoach AI
        </h1>

        <p className="mt-6 text-lg sm:text-xl text-gray-300 leading-relaxed">
          Your personal AI career partner — analyze your CV, match it to jobs,
          and get real-time feedback that helps you get hired faster.
        </p>

        <div className="mt-10 flex justify-center gap-4">
          <Link
            href="/dashboard"
            className="px-6 py-3 bg-blue-600 hover:bg-blue-700 rounded-xl font-semibold transition-all"
          >
            Launch Dashboard
          </Link>
          <Link
            href="/pricing"
            className="px-6 py-3 bg-transparent border border-gray-600 hover:border-gray-400 rounded-xl font-semibold transition-all"
          >
            See Plans
          </Link>
        </div>
      </motion.div>

      {/* Feature Grid */}
      <div className="mt-24 grid sm:grid-cols-2 lg:grid-cols-3 gap-8 max-w-6xl w-full">
        {[
          {
            title: "ATS Score Analyzer",
            desc: "Instantly evaluate your CV against job posts and optimize keywords for maximum impact.",
            icon: "📊",
          },
          {
            title: "AI Bullet Rewrite",
            desc: "Turn weak bullet points into achievement-driven statements employers notice.",
            icon: "🧠",
          },
          {
            title: "Interview Prep",
            desc: "Simulate interviews with AI and get personalized feedback to master your delivery.",
            icon: "🎯",
          },
        ].map((f, i) => (
          <motion.div
            key={i}
            initial={{ opacity: 0, y: 40 }}
            whileInView={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: i * 0.2 }}
            viewport={{ once: true }}
            className="bg-gradient-to-br from-slate-800 to-slate-900 p-6 rounded-2xl shadow-lg border border-slate-700"
          >
            <div className="text-3xl mb-3">{f.icon}</div>
            <h3 className="text-xl font-bold mb-2">{f.title}</h3>
            <p className="text-gray-400 text-sm">{f.desc}</p>
          </motion.div>
        ))}
      </div>

      {/* Footer */}
      <footer className="mt-24 text-sm text-gray-500 text-center">
        © {new Date().getFullYear()} CareerCoach AI. Built for the future of hiring.
      </footer>
    </div>
  );
}
