import { useEffect, useState } from "react";
import Navbar from "../components/Navbar";
import {
  deleteDocument,
  getDocuments,
  uploadDocument,
} from "../api/documentApi";
import type { DocumentResponse } from "../types/api";

const DocumentsPage = () => {
  const [documents, setDocuments] = useState<DocumentResponse[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);

  const [file, setFile] = useState<File | null>(null);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [tags, setTags] = useState("");
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState("");

  const loadDocuments = async () => {
    setLoading(true);

    try {
      const response = await getDocuments(page, 10);

      setDocuments(response.items);
      setTotalPages(response.totalPages);
    } catch {
      setError("Unable to load documents.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadDocuments();
  }, [page]);

  const handleUpload = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!file) {
      setError("Please select a file.");
      return;
    }

    setUploading(true);
    setError("");

    try {
      await uploadDocument(
        file,
        title,
        description,
        tags
      );

      setFile(null);
      setTitle("");
      setDescription("");
      setTags("");

      await loadDocuments();
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
        "Upload failed."
      );
    } finally {
      setUploading(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm("Delete this document?")) return;

    try {
      await deleteDocument(id);
      await loadDocuments();
    } catch {
      setError("Unable to delete document.");
    }
  };

  return (
    <>
      <Navbar />

      <main className="page">
        <div className="page-header">
          <h1>Documents</h1>
          <p>Upload and manage your documents.</p>
        </div>

        <section className="upload-card">
          <h2>Upload Document</h2>

          <form onSubmit={handleUpload}>
            <input
              type="file"
              accept=".pdf"
              onChange={(e) =>
                setFile(e.target.files?.[0] || null)
              }
              required
            />

            <input
              placeholder="Title"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
            />

            <textarea
              placeholder="Description"
              value={description}
              onChange={(e) =>
                setDescription(e.target.value)
              }
            />

            <input
              placeholder="Tags"
              value={tags}
              onChange={(e) => setTags(e.target.value)}
            />

            <button disabled={uploading}>
              {uploading ? "Uploading..." : "Upload"}
            </button>
          </form>
        </section>

        {error && <div className="error">{error}</div>}

        <section>
          <h2>Your Documents</h2>

          {loading ? (
            <div className="loading">Loading...</div>
          ) : documents.length === 0 ? (
            <div className="empty">
              No documents uploaded yet.
            </div>
          ) : (
            <div className="document-list">
              {documents.map((doc) => (
                <div className="document-card" key={doc.id}>
                  <div>
                    <h3>
                      {doc.title || doc.originalFileName}
                    </h3>

                    <p>{doc.originalFileName}</p>

                    <span className="status">
                      {doc.status}
                    </span>

                    {doc.description && (
                      <p>{doc.description}</p>
                    )}
                  </div>

                  <button
                    className="danger"
                    onClick={() => handleDelete(doc.id)}
                  >
                    Delete
                  </button>
                </div>
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
        </section>
      </main>
    </>
  );
};

export default DocumentsPage;