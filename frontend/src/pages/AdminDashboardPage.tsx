import {
  useEffect,
  useState,
} from "react";

import { useAuth } from "../auth/AuthContext";

import {
  getAdminUsers,
  deleteAdminUser,
  getAdminDocuments,
  deleteAdminDocument,
  uploadAdminDocument,
  askAdminDocuments,
  getAdminQueryHistory,
  getAdminQueryHistoryById,
  type AdminUser,
  type AdminDocument,
  type AdminQueryHistory,
  type AdminQueryResponse,
} from "../api/adminApi";

type Section =
  | "users"
  | "documents"
  | "query"
  | "history";

const AdminDashboardPage = () => {
  const { logout } = useAuth();

  const [section, setSection] =
    useState<Section>("users");

  const [error, setError] =
    useState("");

  const [message, setMessage] =
    useState("");

  /*
   * =========================
   * USERS
   * =========================
   */

  const [users, setUsers] =
    useState<AdminUser[]>([]);

  const [userSearch, setUserSearch] =
    useState("");

  const [userPage, setUserPage] =
    useState(1);

  const [userTotalPages, setUserTotalPages] =
    useState(0);

  const [userLoading, setUserLoading] =
    useState(false);

  const [deletingUserId, setDeletingUserId] =
    useState<string | null>(null);

  /*
   * =========================
   * DOCUMENTS
   * =========================
   */

  const [documents, setDocuments] =
    useState<AdminDocument[]>([]);

  const [documentSearch, setDocumentSearch] =
    useState("");

  const [documentStatus, setDocumentStatus] =
    useState("");

  const [documentUploader, setDocumentUploader] =
    useState("");

  const [documentPage, setDocumentPage] =
    useState(1);

  const [documentTotalPages, setDocumentTotalPages] =
    useState(0);

  const [documentLoading, setDocumentLoading] =
    useState(false);

  const [deletingDocumentId, setDeletingDocumentId] =
    useState<string | null>(null);

  /*
   * =========================
   * UPLOAD
   * =========================
   */

  const [uploadFile, setUploadFile] =
    useState<File | null>(null);

  const [uploadOwner, setUploadOwner] =
    useState("");

  const [uploadTitle, setUploadTitle] =
    useState("");

  const [uploadDescription, setUploadDescription] =
    useState("");

  const [uploadTags, setUploadTags] =
    useState("");

  const [uploading, setUploading] =
    useState(false);

  /*
   * =========================
   * ADMIN RAG QUERY
   * =========================
   */

  const [query, setQuery] =
    useState("");

  const [topK, setTopK] =
    useState(5);

  const [queryResult, setQueryResult] =
    useState<AdminQueryResponse | null>(
      null
    );

  const [queryLoading, setQueryLoading] =
    useState(false);

  /*
   * =========================
   * QUERY HISTORY
   * =========================
   */

  const [history, setHistory] =
    useState<AdminQueryHistory[]>([]);

  const [historySearch, setHistorySearch] =
    useState("");

  const [historyUserId, setHistoryUserId] =
    useState("");

  const [historyFromDate, setHistoryFromDate] =
    useState("");

  const [historyToDate, setHistoryToDate] =
    useState("");

  const [historyPage, setHistoryPage] =
    useState(1);

  const [historyTotalPages, setHistoryTotalPages] =
    useState(0);

  const [historyLoading, setHistoryLoading] =
    useState(false);

  const [selectedHistory, setSelectedHistory] =
    useState<AdminQueryHistory | null>(
      null
    );

  /*
   * =========================
   * COMMON
   * =========================
   */

  const clearMessages = () => {
    setError("");
    setMessage("");
  };

  /*
   * =========================
   * LOAD USERS
   * =========================
   */

  const loadUsers = async () => {
    try {
      setUserLoading(true);
      setError("");

      const result =
        await getAdminUsers(
          userPage,
          10,
          userSearch
        );

      setUsers(result.items);

      setUserTotalPages(
        result.totalPages
      );
    } catch (err: any) {
      setError(
        err.response?.status === 403
          ? "You are not authorized to access admin users."
          : err.response?.data?.message ||
            "Failed to load users."
      );
    } finally {
      setUserLoading(false);
    }
  };

  /*
   * =========================
   * LOAD DOCUMENTS
   * =========================
   */

  const loadDocuments = async () => {
    try {
      setDocumentLoading(true);
      setError("");

      const result =
        await getAdminDocuments(
          documentPage,
          10,
          documentSearch,
          documentStatus,
          documentUploader
        );

      setDocuments(result.items);

      setDocumentTotalPages(
        result.totalPages
      );
    } catch (err: any) {
      setError(
        err.response?.status === 403
          ? "You are not authorized to access admin documents."
          : err.response?.data?.message ||
            "Failed to load documents."
      );
    } finally {
      setDocumentLoading(false);
    }
  };

  /*
   * =========================
   * LOAD QUERY HISTORY
   * =========================
   */

  const loadHistory = async () => {
    try {
      setHistoryLoading(true);
      setError("");

      const result =
        await getAdminQueryHistory({
          page: historyPage,
          pageSize: 10,
          search: historySearch,
          userId: historyUserId,
          fromDate: historyFromDate,
          toDate: historyToDate
            ? `${historyToDate}T23:59:59`
            : "",
        });

      setHistory(result.items);

      setHistoryTotalPages(
        result.totalPages
      );
    } catch (err: any) {
      setError(
        err.response?.status === 403
          ? "You are not authorized to access query history."
          : err.response?.data?.message ||
            "Failed to load query history."
      );
    } finally {
      setHistoryLoading(false);
    }
  };

  /*
   * =========================
   * EFFECTS
   * =========================
   */

  useEffect(() => {
    if (section === "users") {
      loadUsers();
    }
  }, [
    section,
    userPage,
  ]);

  useEffect(() => {
    if (section === "documents") {
      loadDocuments();

      /*
       * Make sure the uploader dropdown
       * has users even if the admin directly
       * opens the Documents section.
       */
      if (users.length === 0) {
        getAdminUsers(
          1,
          100,
          ""
        )
          .then((result) => {
            setUsers(result.items);
          })
          .catch(() => {
            // Document loading error is already
            // handled by loadDocuments().
          });
      }
    }
  }, [
    section,
    documentPage,
    documentStatus,
    documentUploader,
  ]);

  useEffect(() => {
    if (section === "history") {
      loadHistory();

      /*
       * Make sure the user filter has users
       * available even when opening History directly.
       */
      if (users.length === 0) {
        getAdminUsers(
          1,
          100,
          ""
        )
          .then((result) => {
            setUsers(result.items);
          })
          .catch(() => {
            // Ignore here. History loading has
            // its own error handling.
          });
      }
    }
  }, [
    section,
    historyPage,
    historyUserId,
    historyFromDate,
    historyToDate,
  ]);

  /*
   * =========================
   * SEARCH USERS
   * =========================
   */

  const searchUsers = async () => {
    try {
      setUserLoading(true);
      setError("");

      const result =
        await getAdminUsers(
          1,
          10,
          userSearch
        );

      setUserPage(1);

      setUsers(result.items);

      setUserTotalPages(
        result.totalPages
      );
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
          "Failed to search users."
      );
    } finally {
      setUserLoading(false);
    }
  };

  /*
   * =========================
   * SEARCH DOCUMENTS
   * =========================
   */

  const searchDocuments = async () => {
    try {
      setDocumentLoading(true);
      setError("");

      const result =
        await getAdminDocuments(
          1,
          10,
          documentSearch,
          documentStatus,
          documentUploader
        );

      setDocumentPage(1);

      setDocuments(result.items);

      setDocumentTotalPages(
        result.totalPages
      );
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
          "Failed to search documents."
      );
    } finally {
      setDocumentLoading(false);
    }
  };

  /*
   * =========================
   * SEARCH QUERY HISTORY
   * =========================
   */

  const searchHistory = async () => {
    try {
      setHistoryLoading(true);
      setError("");

      const result =
        await getAdminQueryHistory({
          page: 1,
          pageSize: 10,
          search: historySearch,
          userId: historyUserId,
          fromDate: historyFromDate,
          toDate: historyToDate
            ? `${historyToDate}T23:59:59`
            : "",
        });

      setHistoryPage(1);

      setHistory(result.items);

      setHistoryTotalPages(
        result.totalPages
      );
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
          "Failed to search query history."
      );
    } finally {
      setHistoryLoading(false);
    }
  };

  /*
   * =========================
   * DELETE USER
   * =========================
   */

  const handleDeleteUser = async (
    user: AdminUser
  ) => {
    const confirmed =
      window.confirm(
        `Are you sure you want to delete ${user.email}?\n\nThis will permanently delete the user and their related documents and query history.`
      );

    if (!confirmed) {
      return;
    }

    try {
      clearMessages();

      setDeletingUserId(
        user.id
      );

      await deleteAdminUser(
        user.id
      );

      setMessage(
        `User ${user.email} was deleted successfully.`
      );

      /*
       * Remove the user immediately from
       * the current table.
       */
      setUsers((currentUsers) =>
        currentUsers.filter(
          (item) =>
            item.id !== user.id
        )
      );

      /*
       * Reload the current page so pagination
       * and counts stay correct.
       */
      await loadUsers();

      /*
       * If the deleted user was selected as
       * a document uploader, clear the filter.
       */
      if (
        documentUploader === user.id
      ) {
        setDocumentUploader("");
      }

      /*
       * If the deleted user was selected in
       * query history, clear that filter.
       */
      if (
        historyUserId === user.id
      ) {
        setHistoryUserId("");
      }
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
          err.response?.data?.detail ||
          "Failed to delete user."
      );
    } finally {
      setDeletingUserId(null);
    }
  };

  /*
   * =========================
   * DELETE DOCUMENT
   * =========================
   */

  const handleDeleteDocument = async (
    document: AdminDocument
  ) => {
    const confirmed =
      window.confirm(
        `Are you sure you want to delete "${document.originalFileName}"?\n\nThis will permanently delete the document and its processed data.`
      );

    if (!confirmed) {
      return;
    }

    try {
      clearMessages();

      setDeletingDocumentId(
        document.id
      );

      await deleteAdminDocument(
        document.id
      );

      setMessage(
        `Document "${document.originalFileName}" was deleted successfully.`
      );

      /*
       * Remove it immediately from UI.
       */
      setDocuments((currentDocuments) =>
        currentDocuments.filter(
          (item) =>
            item.id !== document.id
        )
      );

      /*
       * Reload to keep pagination accurate.
       */
      await loadDocuments();
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
          err.response?.data?.detail ||
          "Failed to delete document."
      );
    } finally {
      setDeletingDocumentId(null);
    }
  };

  /*
   * =========================
   * UPLOAD DOCUMENT
   * =========================
   */

  const handleUpload = async (
    e: React.FormEvent
  ) => {
    e.preventDefault();

    clearMessages();

    if (!uploadFile) {
      setError(
        "Please select a file."
      );
      return;
    }

    if (!uploadOwner) {
      setError(
        "Please select a document owner."
      );
      return;
    }

    try {
      setUploading(true);

      await uploadAdminDocument(
        uploadFile,
        uploadOwner,
        uploadTitle,
        uploadDescription,
        uploadTags
      );

      setMessage(
        "Document uploaded successfully. Processing will happen asynchronously."
      );

      setUploadFile(null);
      setUploadOwner("");
      setUploadTitle("");
      setUploadDescription("");
      setUploadTags("");

      const input =
        document.getElementById(
          "admin-upload-file"
        ) as HTMLInputElement | null;

      if (input) {
        input.value = "";
      }

      await loadDocuments();
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
          err.response?.data?.detail ||
          "Failed to upload document."
      );
    } finally {
      setUploading(false);
    }
  };

  /*
   * =========================
   * ADMIN RAG QUERY
   * =========================
   */

  const handleAdminQuery = async (
    e: React.FormEvent
  ) => {
    e.preventDefault();

    clearMessages();

    setQueryResult(null);

    if (!query.trim()) {
      setError(
        "Please enter a question."
      );
      return;
    }

    try {
      setQueryLoading(true);

      const result =
        await askAdminDocuments(
          query.trim(),
          topK
        );

      setQueryResult(result);
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
          err.response?.data?.detail ||
          "Failed to process the query."
      );
    } finally {
      setQueryLoading(false);
    }
  };

  /*
   * =========================
   * QUERY HISTORY DETAILS
   * =========================
   */

  const handleHistoryDetails = async (
    id: string
  ) => {
    try {
      clearMessages();

      const result =
        await getAdminQueryHistoryById(
          id
        );

      setSelectedHistory(
        result
      );
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
          "Failed to load query history details."
      );
    }
  };

  /*
   * =========================
   * PAGINATION
   * =========================
   */

  const renderPagination = (
    page: number,
    totalPages: number,
    setPage: (
      value: number
    ) => void
  ) => {
    if (totalPages <= 1) {
      return null;
    }

    return (
      <div className="admin-pagination">
        <button
          disabled={page <= 1}
          onClick={() =>
            setPage(page - 1)
          }
        >
          Previous
        </button>

        <span>
          Page {page} of{" "}
          {totalPages}
        </span>

        <button
          disabled={
            page >= totalPages
          }
          onClick={() =>
            setPage(page + 1)
          }
        >
          Next
        </button>
      </div>
    );
  };

  /*
   * =========================
   * RENDER
   * =========================
   */

  return (
    <div className="admin-page">
      <header className="admin-header">
        <div>
          <h1>
            AI Document Intelligence
          </h1>

          <span>
            Admin Panel
          </span>
        </div>

        <button
          className="admin-logout"
          onClick={logout}
        >
          Logout
        </button>
      </header>

      <div className="admin-layout">
        <aside className="admin-sidebar">
          <button
            className={
              section === "users"
                ? "active"
                : ""
            }
            onClick={() => {
              clearMessages();
              setSection("users");
            }}
          >
            👥 Users
          </button>

          <button
            className={
              section === "documents"
                ? "active"
                : ""
            }
            onClick={() => {
              clearMessages();
              setSection("documents");
            }}
          >
            📄 Documents
          </button>

          <button
            className={
              section === "query"
                ? "active"
                : ""
            }
            onClick={() => {
              clearMessages();
              setSection("query");
            }}
          >
            🤖 Ask Documents
          </button>

          <button
            className={
              section === "history"
                ? "active"
                : ""
            }
            onClick={() => {
              clearMessages();
              setSection("history");
            }}
          >
            🕘 Query History
          </button>
        </aside>

        <main className="admin-content">
          {error && (
            <div className="admin-error">
              {error}
            </div>
          )}

          {message && (
            <div className="admin-success">
              {message}
            </div>
          )}

          {/* =========================
              USERS
              ========================= */}

          {section === "users" && (
            <section>
              <div className="admin-title-row">
                <div>
                  <h2>
                    Users
                  </h2>

                  <p>
                    Manage registered users.
                  </p>
                </div>
              </div>

              <div className="admin-toolbar">
                <input
                  placeholder="Search users..."
                  value={userSearch}
                  onChange={(e) =>
                    setUserSearch(
                      e.target.value
                    )
                  }
                  onKeyDown={(e) => {
                    if (
                      e.key === "Enter"
                    ) {
                      searchUsers();
                    }
                  }}
                />

                <button
                  onClick={
                    searchUsers
                  }
                >
                  Search
                </button>
              </div>

              {userLoading ? (
                <p>
                  Loading users...
                </p>
              ) : users.length === 0 ? (
                <div className="admin-empty">
                  No users found.
                </div>
              ) : (
                <div className="admin-table-wrapper">
                  <table className="admin-table">
                    <thead>
                      <tr>
                        <th>
                          Email
                        </th>

                        <th>
                          Username
                        </th>

                        <th>
                          Role
                        </th>

                        <th>
                          Created
                        </th>

                        <th>
                          Action
                        </th>
                      </tr>
                    </thead>

                    <tbody>
                      {users.map(
                        (user) => (
                          <tr
                            key={
                              user.id
                            }
                          >
                            <td>
                              {
                                user.email
                              }
                            </td>

                            <td>
                              {
                                user.userName
                              }
                            </td>

                            <td>
                              <span className="admin-badge">
                                {
                                  user.role
                                }
                              </span>
                            </td>

                            <td>
                              {new Date(
                                user.createdAt
                              ).toLocaleString()}
                            </td>

                            <td>
                              <button
                                className="danger-button"
                                disabled={
                                  deletingUserId ===
                                  user.id
                                }
                                onClick={() =>
                                  handleDeleteUser(
                                    user
                                  )
                                }
                              >
                                {deletingUserId ===
                                user.id
                                  ? "Deleting..."
                                  : "Delete"}
                              </button>
                            </td>
                          </tr>
                        )
                      )}
                    </tbody>
                  </table>
                </div>
              )}

              {renderPagination(
                userPage,
                userTotalPages,
                setUserPage
              )}
            </section>
          )}

          {/* =========================
              DOCUMENTS
              ========================= */}

          {section === "documents" && (
            <section>
              <div className="admin-title-row">
                <div>
                  <h2>
                    Documents
                  </h2>

                  <p>
                    Search, upload and manage documents.
                  </p>
                </div>
              </div>

              <div className="admin-toolbar">
                <input
                  placeholder="Search documents..."
                  value={
                    documentSearch
                  }
                  onChange={(e) =>
                    setDocumentSearch(
                      e.target.value
                    )
                  }
                  onKeyDown={(e) => {
                    if (
                      e.key === "Enter"
                    ) {
                      searchDocuments();
                    }
                  }}
                />

                <select
                  value={
                    documentStatus
                  }
                  onChange={(e) => {
                    setDocumentStatus(
                      e.target.value
                    );
                    setDocumentPage(1);
                  }}
                >
                  <option value="">
                    All statuses
                  </option>

                  <option value="Uploaded">
                    Uploaded
                  </option>

                  <option value="Processing">
                    Processing
                  </option>

                  <option value="Ready">
                    Ready
                  </option>

                  <option value="Failed">
                    Failed
                  </option>
                </select>

                <select
                  value={
                    documentUploader
                  }
                  onChange={(e) => {
                    setDocumentUploader(
                      e.target.value
                    );
                    setDocumentPage(1);
                  }}
                >
                  <option value="">
                    All uploaders
                  </option>

                  {users.map(
                    (user) => (
                      <option
                        key={
                          user.id
                        }
                        value={
                          user.id
                        }
                      >
                        {
                          user.email
                        }
                      </option>
                    )
                  )}
                </select>

                <button
                  onClick={
                    searchDocuments
                  }
                >
                  Search
                </button>
              </div>

              {/* UPLOAD */}

              <div className="admin-card">
                <h3>
                  Upload Document
                </h3>

                <form
                  onSubmit={
                    handleUpload
                  }
                  className="admin-form"
                >
                  <label>
                    File

                    <input
                      id="admin-upload-file"
                      type="file"
                      onChange={(
                        e
                      ) =>
                        setUploadFile(
                          e.target
                            .files?.[0] ??
                            null
                        )
                      }
                      required
                    />
                  </label>

                  <label>
                    Owner

                    <select
                      value={
                        uploadOwner
                      }
                      onChange={(e) =>
                        setUploadOwner(
                          e.target.value
                        )
                      }
                      required
                    >
                      <option value="">
                        Select owner
                      </option>

                      {users.map(
                        (user) => (
                          <option
                            key={
                              user.id
                            }
                            value={
                              user.id
                            }
                          >
                            {
                              user.email
                            }
                          </option>
                        )
                      )}
                    </select>
                  </label>

                  <label>
                    Title

                    <input
                      value={
                        uploadTitle
                      }
                      onChange={(e) =>
                        setUploadTitle(
                          e.target.value
                        )
                      }
                      placeholder="Document title"
                    />
                  </label>

                  <label>
                    Description

                    <textarea
                      value={
                        uploadDescription
                      }
                      onChange={(e) =>
                        setUploadDescription(
                          e.target.value
                        )
                      }
                      placeholder="Document description"
                    />
                  </label>

                  <label>
                    Tags

                    <input
                      value={
                        uploadTags
                      }
                      onChange={(e) =>
                        setUploadTags(
                          e.target.value
                        )
                      }
                      placeholder="policy, HR, leave"
                    />
                  </label>

                  <button
                    type="submit"
                    disabled={
                      uploading
                    }
                  >
                    {uploading
                      ? "Uploading..."
                      : "Upload Document"}
                  </button>
                </form>
              </div>

              {/* DOCUMENT LIST */}

              <h3>
                Document List
              </h3>

              {documentLoading ? (
                <p>
                  Loading documents...
                </p>
              ) : documents.length ===
                0 ? (
                <div className="admin-empty">
                  No documents found.
                </div>
              ) : (
                <div className="admin-table-wrapper">
                  <table className="admin-table">
                    <thead>
                      <tr>
                        <th>
                          File
                        </th>

                        <th>
                          Type
                        </th>

                        <th>
                          Status
                        </th>

                        <th>
                          Uploaded
                        </th>

                        <th>
                          Pages
                        </th>

                        <th>
                          Action
                        </th>
                      </tr>
                    </thead>

                    <tbody>
                      {documents.map(
                        (doc) => (
                          <tr
                            key={
                              doc.id
                            }
                          >
                            <td>
                              <strong>
                                {
                                  doc.originalFileName
                                }
                              </strong>

                              <br />

                              {doc.title && (
                                <small>
                                  {
                                    doc.title
                                  }
                                </small>
                              )}
                            </td>

                            <td>
                              {
                                doc.contentType
                              }
                            </td>

                            <td>
                              <span className="admin-badge">
                                {
                                  doc.status
                                }
                              </span>

                              {doc.statusMessage && (
                                <>
                                  <br />

                                  <small>
                                    {
                                      doc.statusMessage
                                    }
                                  </small>
                                </>
                              )}
                            </td>

                            <td>
                              {new Date(
                                doc.uploadedAt
                              ).toLocaleString()}
                            </td>

                            <td>
                              {
                                doc.pageCount ??
                                "-"
                              }
                            </td>

                            <td>
                              <button
                                className="danger-button"
                                disabled={
                                  deletingDocumentId ===
                                  doc.id
                                }
                                onClick={() =>
                                  handleDeleteDocument(
                                    doc
                                  )
                                }
                              >
                                {deletingDocumentId ===
                                doc.id
                                  ? "Deleting..."
                                  : "Delete"}
                              </button>
                            </td>
                          </tr>
                        )
                      )}
                    </tbody>
                  </table>
                </div>
              )}

              {renderPagination(
                documentPage,
                documentTotalPages,
                setDocumentPage
              )}
            </section>
          )}

          {/* =========================
              ADMIN RAG QUERY
              ========================= */}

          {section === "query" && (
            <section>
              <h2>
                Ask Documents
              </h2>

              <p>
                Ask questions across the available document corpus.
              </p>

              <form
                onSubmit={
                  handleAdminQuery
                }
                className="admin-card admin-query-form"
              >
                <textarea
                  value={query}
                  onChange={(e) =>
                    setQuery(
                      e.target.value
                    )
                  }
                  placeholder="Ask something about your documents..."
                  rows={5}
                />

                <label>
                  Top K

                  <input
                    type="number"
                    min={1}
                    max={20}
                    value={topK}
                    onChange={(e) =>
                      setTopK(
                        Math.min(
                          20,
                          Math.max(
                            1,
                            Number(
                              e.target
                                .value
                            )
                          )
                        )
                      )
                    }
                  />
                </label>

                <button
                  type="submit"
                  disabled={
                    queryLoading
                  }
                >
                  {queryLoading
                    ? "Thinking..."
                    : "Ask Documents"}
                </button>
              </form>

              {queryResult && (
                <div className="admin-card">
                  <h3>
                    Answer
                  </h3>

                  <div className="admin-answer">
                    {
                      queryResult.answer
                    }
                  </div>

                  <h3>
                    Sources
                  </h3>

                  {queryResult.sources
                    .length ===
                  0 ? (
                    <p>
                      No relevant sources were returned.
                    </p>
                  ) : (
                    <div>
                      {queryResult.sources.map(
                        (
                          source,
                          index
                        ) => (
                          <div
                            className="source-card"
                            key={
                              source.documentChunkId
                            }
                          >
                            <strong>
                              Source{" "}
                              {index +
                                1}
                            </strong>

                            <div>
                              <strong>
                                Similarity:
                              </strong>{" "}
                              {source.similarityScore.toFixed(
                                3
                              )}
                            </div>

                            <div>
                              <strong>
                                Page:
                              </strong>{" "}
                              {source.pageNumber ??
                                "-"}
                            </div>

                            <p>
                              {
                                source.content
                              }
                            </p>
                          </div>
                        )
                      )}
                    </div>
                  )}
                </div>
              )}
            </section>
          )}

          {/* =========================
              QUERY HISTORY
              ========================= */}

          {section === "history" && (
            <section>
              <h2>
                Query History
              </h2>

              <p>
                View and filter queries from all users.
              </p>

              <div className="admin-toolbar admin-history-filters">
                <input
                  placeholder="Search query..."
                  value={
                    historySearch
                  }
                  onChange={(e) =>
                    setHistorySearch(
                      e.target.value
                    )
                  }
                  onKeyDown={(e) => {
                    if (
                      e.key === "Enter"
                    ) {
                      searchHistory();
                    }
                  }}
                />

                <select
                  value={
                    historyUserId
                  }
                  onChange={(e) => {
                    setHistoryUserId(
                      e.target.value
                    );
                    setHistoryPage(1);
                  }}
                >
                  <option value="">
                    All users
                  </option>

                  {users.map(
                    (user) => (
                      <option
                        key={
                          user.id
                        }
                        value={
                          user.id
                        }
                      >
                        {
                          user.email
                        }
                      </option>
                    )
                  )}
                </select>

                <input
                  type="date"
                  value={
                    historyFromDate
                  }
                  onChange={(e) => {
                    setHistoryFromDate(
                      e.target.value
                    );
                    setHistoryPage(1);
                  }}
                />

                <input
                  type="date"
                  value={
                    historyToDate
                  }
                  onChange={(e) => {
                    setHistoryToDate(
                      e.target.value
                    );
                    setHistoryPage(1);
                  }}
                />

                <button
                  onClick={
                    searchHistory
                  }
                >
                  Search
                </button>
              </div>

              {historyLoading ? (
                <p>
                  Loading query history...
                </p>
              ) : history.length ===
                0 ? (
                <div className="admin-empty">
                  No query history found.
                </div>
              ) : (
                <div className="admin-table-wrapper">
                  <table className="admin-table">
                    <thead>
                      <tr>
                        <th>
                          Query
                        </th>

                        <th>
                          User
                        </th>

                        <th>
                          Grounded
                        </th>

                        <th>
                          Created
                        </th>

                        <th>
                          Response Time
                        </th>

                        <th>
                          Action
                        </th>
                      </tr>
                    </thead>

                    <tbody>
                      {history.map(
                        (item) => (
                          <tr
                            key={
                              item.id
                            }
                          >
                            <td>
                              {
                                item.query
                              }
                            </td>

                            <td>
                              {
                                item.userId
                              }
                            </td>

                            <td>
                              {item.isGrounded
                                ? "Yes"
                                : "No"}
                            </td>

                            <td>
                              {new Date(
                                item.createdAt
                              ).toLocaleString()}
                            </td>

                            <td>
                              {item.responseTimeMs ??
                                "-"}{" "}
                              ms
                            </td>

                            <td>
                              <button
                                onClick={() =>
                                  handleHistoryDetails(
                                    item.id
                                  )
                                }
                              >
                                Details
                              </button>
                            </td>
                          </tr>
                        )
                      )}
                    </tbody>
                  </table>
                </div>
              )}

              {renderPagination(
                historyPage,
                historyTotalPages,
                setHistoryPage
              )}

              {selectedHistory && (
                <div className="admin-card">
                  <div className="admin-title-row">
                    <h3>
                      Query Details
                    </h3>

                    <button
                      onClick={() =>
                        setSelectedHistory(
                          null
                        )
                      }
                    >
                      Close
                    </button>
                  </div>

                  <p>
                    <strong>
                      User:
                    </strong>{" "}
                    {
                      selectedHistory.userId
                    }
                  </p>

                  <p>
                    <strong>
                      Query:
                    </strong>{" "}
                    {
                      selectedHistory.query
                    }
                  </p>

                  <p>
                    <strong>
                      Answer:
                    </strong>
                  </p>

                  <div className="admin-answer">
                    {
                      selectedHistory.answer
                    }
                  </div>

                  <p>
                    <strong>
                      Grounded:
                    </strong>{" "}
                    {selectedHistory.isGrounded
                      ? "Yes"
                      : "No"}
                  </p>

                  <p>
                    <strong>
                      Response time:
                    </strong>{" "}
                    {
                      selectedHistory.responseTimeMs ??
                      "-"
                    }{" "}
                    ms
                  </p>

                  <h4>
                    Sources
                  </h4>

                  {selectedHistory.sources
                    .length ===
                  0 ? (
                    <p>
                      No sources.
                    </p>
                  ) : (
                    selectedHistory.sources.map(
                      (source) => (
                        <div
                          className="source-card"
                          key={
                            source.documentChunkId
                          }
                        >
                          <div>
                            <strong>
                              Chunk:
                            </strong>{" "}
                            {
                              source.documentChunkId
                            }
                          </div>

                          <div>
                            <strong>
                              Relevance:
                            </strong>{" "}
                            {source.relevanceScore.toFixed(
                              3
                            )}
                          </div>
                        </div>
                      )
                    )
                  )}
                </div>
              )}
            </section>
          )}
        </main>
      </div>
    </div>
  );
};

export default AdminDashboardPage;