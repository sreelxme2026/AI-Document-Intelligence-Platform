import { Link } from "react-router-dom";
import Navbar from "../components/Navbar";

const DashboardPage = () => {
  return (
    <>
      <Navbar />

      <main className="dashboard">
        <h1>Welcome to AI Document Intelligence</h1>
        <p>
          Upload documents and ask questions using AI-powered
          document retrieval.
        </p>

        <div className="dashboard-grid">
          <Link to="/documents" className="dashboard-card">
            <h2>📄 Documents</h2>
            <p>Upload and manage your documents.</p>
          </Link>

          <Link to="/query" className="dashboard-card">
            <h2>🤖 Ask AI</h2>
            <p>Ask questions about your documents.</p>
          </Link>

          <Link to="/history" className="dashboard-card">
            <h2>🕒 Query History</h2>
            <p>View your previous AI questions and answers.</p>
          </Link>
        </div>
      </main>
    </>
  );
};

export default DashboardPage;