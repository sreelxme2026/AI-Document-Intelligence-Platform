# AI Document Intelligence Platform

An AI-powered document understanding and retrieval platform that allows users to upload documents, ask natural-language questions, and receive grounded answers based on the uploaded document content.

The platform uses a **Retrieval-Augmented Generation (RAG)** pipeline to retrieve relevant document passages before generating an answer with **Google Gemini**.

---

## 📌 Project Overview

The AI Document Intelligence Platform is designed to turn a collection of difficult-to-search documents into searchable knowledge.

Traditional keyword search can fail when a user's question is phrased differently from the wording used inside a document. Manually searching through long PDFs is also time-consuming.

This platform solves that problem by:

1. Accepting document uploads.
2. Extracting text from documents.
3. Splitting documents into smaller chunks.
4. Generating embeddings for each chunk.
5. Storing the embeddings for similarity-based retrieval.
6. Converting user questions into embeddings.
7. Finding the most relevant document chunks using cosine similarity.
8. Sending the retrieved context to Google Gemini.
9. Generating a grounded answer using the retrieved evidence.
10. Returning the answer together with its sources.
11. Maintaining query history for traceability.

### Core Principle

> **Retrieval supplies the knowledge; Gemini supplies the natural-language generation.**

The model is instructed to use only the retrieved document context and to avoid inventing information.

---

# 🎯 Key Features

## 👤 User Features

- User registration and login
- JWT-based authentication
- Upload documents
- View uploaded documents
- Ask natural-language questions
- Receive AI-generated answers
- View answer sources
- View personal query history
- Track document processing status

## 🛡️ Admin Features

Administrators can:

- List users
- Search users
- Delete users
- List documents
- Search documents
- View document details
- Upload documents
- Delete documents
- Query across documents
- View query history
- Search/filter query history
- View query sources and relevance scores

All admin APIs are protected by server-side role authorization.

---

# 🏗️ System Architecture

```text
┌──────────────────────────────┐
│       React Frontend         │
│      React + TypeScript      │
└──────────────┬───────────────┘
               │ REST API
               ▼
┌──────────────────────────────┐
│      ASP.NET Core API        │
│          .NET 8              │
│  Controllers + Authentication│
└──────────────┬───────────────┘
               ▼
┌──────────────────────────────┐
│      Application Layer       │
│ DTOs • Entities • Interfaces │
│      Service Contracts       │
└──────────────┬───────────────┘
               ▼
┌──────────────────────────────┐
│     Infrastructure Layer     │
│ EF Core • Identity • RAG     │
│ File Storage • AI Processing │
└───────┬─────────┬────────────┘
        │         │
        ▼         ▼
   SQL Server   Google Gemini
        │
        ▼
   File Storage
