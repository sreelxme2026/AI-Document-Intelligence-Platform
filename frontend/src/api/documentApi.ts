import api from "./client";
import type {
  DocumentListResponse,
  DocumentResponse,
  DocumentStatusResponse,
} from "../types/api";

export const getDocuments = async (
  page = 1,
  pageSize = 10
): Promise<DocumentListResponse> => {
  const response = await api.get<DocumentListResponse>(
    "/documents",
    {
      params: {
        page,
        pageSize,
      },
    }
  );

  return response.data;
};

export const getDocument = async (
  id: string
): Promise<DocumentResponse> => {
  const response = await api.get<DocumentResponse>(
    `/documents/${id}`
  );

  return response.data;
};

export const uploadDocument = async (
  file: File,
  title: string,
  description: string,
  tags: string
): Promise<DocumentResponse> => {
  const formData = new FormData();

  formData.append("file", file);
  formData.append("title", title);
  formData.append("description", description);
  formData.append("tags", tags);

  const response = await api.post<DocumentResponse>(
    "/documents",
    formData
  );

  return response.data;
};

export const getDocumentStatus = async (
  id: string
): Promise<DocumentStatusResponse> => {
  const response = await api.get<DocumentStatusResponse>(
    `/documents/${id}/status`
  );

  return response.data;
};

export const deleteDocument = async (
  id: string
): Promise<void> => {
  await api.delete(`/documents/${id}`);
};