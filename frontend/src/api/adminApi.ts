import api from "./client";

export interface AdminUser {
  id: string;
  email: string;
  userName: string;
  role: string;
  createdAt: string;
}

export interface AdminUserListResponse {
  items: AdminUser[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AdminDocument {
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

export interface AdminDocumentListResponse {
  items: AdminDocument[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface RetrievalSource {
  documentChunkId: string;
  documentId: string;
  content: string;
  pageNumber: number | null;
  similarityScore: number;
}

export interface AdminQueryResponse {
  answer: string;
  sources: RetrievalSource[];
}

export interface QueryHistorySource {
  documentChunkId: string;
  relevanceScore: number;
}

export interface AdminQueryHistory {
  id: string;
  userId: string;
  query: string;
  answer: string;
  isGrounded: boolean;
  createdAt: string;
  responseTimeMs: number | null;
  sources: QueryHistorySource[];
}

export interface AdminQueryHistoryListResponse {
  items: AdminQueryHistory[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AdminQueryHistoryQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  userId?: string;
  fromDate?: string;
  toDate?: string;
}

export const getAdminUsers = async (
  page = 1,
  pageSize = 10,
  search = ""
) => {
  const response =
    await api.get<AdminUserListResponse>(
      "/admin/users",
      {
        params: {
          page,
          pageSize,
          search: search || undefined,
        },
      }
    );

  return response.data;
};

export const deleteAdminUser = async (
  id: string
) => {
  await api.delete(
    `/admin/users/${id}`
  );
};

export const getAdminDocuments = async (
  page = 1,
  pageSize = 10,
  search = "",
  status = "",
  uploaderId = ""
) => {
  const response =
    await api.get<AdminDocumentListResponse>(
      "/admin/documents",
      {
        params: {
          page,
          pageSize,
          search: search || undefined,
          status: status || undefined,
          uploaderId:
            uploaderId || undefined,
        },
      }
    );

  return response.data;
};

export const deleteAdminDocument = async (
  id: string
) => {
  await api.delete(
    `/admin/documents/${id}`
  );
};

export const uploadAdminDocument = async (
  file: File,
  userId: string,
  title?: string,
  description?: string,
  tags?: string
) => {
  const formData = new FormData();

  formData.append(
    "file",
    file
  );

  formData.append(
    "userId",
    userId
  );

  if (title) {
    formData.append(
      "title",
      title
    );
  }

  if (description) {
    formData.append(
      "description",
      description
    );
  }

  if (tags) {
    formData.append(
      "tags",
      tags
    );
  }

  const response =
    await api.post<AdminDocument>(
      "/admin/documents",
      formData
    );

  return response.data;
};

export const askAdminDocuments = async (
  query: string,
  topK: number
) => {
  const response =
    await api.post<AdminQueryResponse>(
      "/admin/query",
      {
        query,
        topK,
      }
    );

  return response.data;
};

export const getAdminQueryHistory =
  async (
    parameters: AdminQueryHistoryQuery
  ) => {
    const response =
      await api.get<AdminQueryHistoryListResponse>(
        "/admin/query-history",
        {
          params: {
            page:
              parameters.page ?? 1,

            pageSize:
              parameters.pageSize ?? 10,

            search:
              parameters.search || undefined,

            userId:
              parameters.userId || undefined,

            fromDate:
              parameters.fromDate ||
              undefined,

            toDate:
              parameters.toDate ||
              undefined,
          },
        }
      );

    return response.data;
  };

export const getAdminQueryHistoryById =
  async (
    id: string
  ) => {
    const response =
      await api.get<AdminQueryHistory>(
        `/admin/query-history/${id}`
      );

    return response.data;
  };