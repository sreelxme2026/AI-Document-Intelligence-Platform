import { useEffect, useState } from "react";
import Navbar from "../components/Navbar";
import SourceCard from "../components/SourceCard";
import { getHistory } from "../api/historyApi";
import type { QueryHistoryResponse } from "../types/api";

const HistoryPage = () => {
  const [items, setItems] = useState<QueryHistoryResponse[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);

  const loadHistory = async () => {
    setLoading(true);

    try {
      const response = await getHistory(page, 10);

      setItems(response.items);
      setTotalPages(response.totalPages);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadHistory();
  }, [page]);

  return (
    <>
      <Navbar />

      <main className="page">
        <div className="page-header">
          <h1>Query History</h1>
          <p>Your previous AI questions and answers.</p>
        </div>

        {loading ? (
          <div className="loading">Loading...</div>
        ) : items.length === 0 ? (
          <div className="empty">
            No queries yet.
          </div>
        ) : (
          <div className="history-list">
            {items.map((item) => (
              <details className="history-card" key={item.id}>
                <summary>
                  <strong>{item.query}</strong>
                  <span>
                    {item.isGrounded
                      ? " ✓ Grounded"
                      : " Not Grounded"}
                  </span>
                </summary>

                <div className="history-content">
                  <h3>Answer</h3>
                  <p>{item.answer}</p>

                  {item.responseTimeMs !== null && (
                    <small>
                      Response time:{" "}
                      {(item.responseTimeMs / 1000).toFixed(2)}s
                    </small>
                  )}

                  <h3>Sources</h3>

                  {item.sources?.map((source, index) => (
                    <SourceCard
                      key={source.documentChunkId}
                      source={source}
                      index={index}
                    />
                  ))}
                </div>
              </details>
            ))}
          </div>
        )}

        {totalPages > 1 && (
          <div className="pagination">
            <button
              disabled={page === 1}
              onClick={() => setPage(page - 1)}
            >
              Previous
            </button>

            <span>
              Page {page} of {totalPages}
            </span>

            <button
              disabled={page === totalPages}
              onClick={() => setPage(page + 1)}
            >
              Next
            </button>
          </div>
        )}
      </main>
    </>
  );
};

export default HistoryPage;