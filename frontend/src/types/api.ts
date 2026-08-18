export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
}

export interface DocumentResponse {
  id: string;
  fileName: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedByUserId: string;
  status: string;
  statusMessage: string | null;
  uploadedAt: string;
  processedAt: string | null;
  pageCount: number | null;
  title: string | null;
  description: string | null;
  tags: string | null;
}

export interface DocumentListResponse {
  items: DocumentResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface DocumentStatusResponse {
  id: string;
  status: string;
  statusMessage: string | null;
  processedAt: string | null;
}

export interface RetrievalSource {
  documentChunkId: string;
  documentId: string;
  chunkIndex: number;
  content: string;
  pageNumber: number | null;
  similarityScore: number;
}

export interface RagRequest {
  query: string;
  topK: number;
}

export interface RagResult {
  answer: string;
  sources: RetrievalSource[];
}

export interface QueryHistoryResponse {
  id: string;
  userId: string;
  query: string;
  answer: string;
  isGrounded: boolean;
  createdAt: string;
  responseTimeMs: number | null;
  sources: RetrievalSource[];
}

export interface QueryHistoryListResponse {
  items: QueryHistoryResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}