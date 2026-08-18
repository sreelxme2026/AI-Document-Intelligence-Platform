import { useState } from "react";
import Navbar from "../components/Navbar";
import SourceCard from "../components/SourceCard";
import { askQuestion } from "../api/queryApi";
import type { RagResult } from "../types/api";

const QueryPage = () => {
  const [query, setQuery] = useState("");
  const [result, setResult] = useState<RagResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleAsk = async () => {
    if (!query.trim()) return;

    setLoading(true);
    setError("");
    setResult(null);

    try {
      const response = await askQuestion({
        query,
        topK: 20,
      });

      setResult(response);
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
        "Unable to answer the question."
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <Navbar />

      <main className="page">
        <div className="page-header">
          <h1>Ask Your Documents</h1>
          <p>Ask questions and get AI-powered answers.</p>
        </div>

        <div className="query-box">
          <textarea
            placeholder="What would you like to know?"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            rows={4}
          />

          <button
            onClick={handleAsk}
            disabled={loading || !query.trim()}
          >
            {loading ? "Searching your documents..." : "Ask AI"}
          </button>
        </div>

        {error && <div className="error">{error}</div>}

        {loading && (
          <div className="loading">
            Thinking... Searching your documents...
          </div>
        )}

        {result && (
          <section className="result">
            <h2>AI Answer</h2>

            <div className="answer-card">
              {result.answer}
            </div>

            <h2>Sources</h2>

            <div>
              {result.sources.map((source, index) => (
                <SourceCard
                  key={source.documentChunkId}
                  source={source}
                  index={index}
                />
              ))}
            </div>
          </section>
        )}
      </main>
    </>
  );
};

export default QueryPage;