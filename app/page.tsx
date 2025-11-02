"use client";

import { motion } from "framer-motion";
import Link from "next/link";

export default function Home() {
  const features = [
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
  ];

  return (
    <div className="relative min-h-screen flex flex-col items-center justify-center overflow-hidden bg-gradient-to-b from-slate-950 via-slate-900 to-black px-6 py-20 text-white">
      {/* Decorative blurred background */}
      <div className="absolute top-0 left-1/2 w-[120%] h-[120%] -translate-x-1/2 -translate-y-1/4 pointer-events-none">
        <div className="absolute inset-0 bg-gradient-to-tr from-cyan-500/30 via-fuchsia-500/20 to-purple-500/30 rounded-full blur-3xl opacity-40"></div>
      </div>

      <motion.div
        initial={{ opacity: 0, y: 40 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.8 }}
        className="max-w-4xl text-center space-y-6"
      >
        <h1 className="text-5xl sm:text-6xl md:text-7xl font-extrabold bg-clip-text text-transparent bg-gradient-to-r from-cyan-300 via-blue-400 to-purple-500">
          CareerCoach AI
        </h1>
        <p className="text-lg sm:text-xl text-gray-300">
          Your personal AI career partner — analyze your CV, match it to jobs, and get real-time feedback that helps you get hired faster.
        </p>
        <div className="flex flex-wrap justify-center gap-4 pt-2">
          <Link
            href="/dashboard"
            className="px-6 py-3 rounded-full bg-white/10 hover:bg-white/20 backdrop-blur-md border border-white/20 font-semibold transition-colors"
          >
            Launch Dashboard
          </Link>
          <Link
            href="/pricing"
            className="px-6 py-3 rounded-full border border-white/20 hover:bg-white/10 backdrop-blur-md font-semibold transition-colors"
          >
            See Plans
          </Link>
        </div>
      </motion.div>

      <div className="mt-32 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-8 max-w-6xl w-full">
        {features.map((f, i) => (
          <motion.div
            key={i}
            initial={{ opacity: 0, y: 40 }}
            whileInView={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: i * 0.2 }}
            viewport={{ once: true }}
            className="p-6 rounded-3xl bg-white/10 backdrop-blur-lg border border-white/20 shadow-md"
          >
            <div className="text-4xl mb-4">{f.icon}</div>
            <h3 className="text-xl font-semibold mb-2">{f.title}</h3>
            <p className="text-gray-300 text-sm">{f.desc}</p>
          </motion.div>
        ))}
      </div>

      <footer className="mt-24 text-xs text-gray-500 text-center">
        © {new Date().getFullYear()} CareerCoach AI. Built for the future of hiring.
      </footer>
    </div>
  );
}
