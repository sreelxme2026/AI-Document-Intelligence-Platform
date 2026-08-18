import api from "./client";
import type {
  QueryHistoryListResponse,
  QueryHistoryResponse,
} from "../types/api";

export const getHistory = async (
  page = 1,
  pageSize = 10
): Promise<QueryHistoryListResponse> => {
  const response = await api.get<QueryHistoryListResponse>(
    "/query/history",
    {
      params: {
        page,
        pageSize,
      },
    }
  );

  return response.data;
};

export const getHistoryDetails = async (
  id: string
): Promise<QueryHistoryResponse> => {
  const response = await api.get<QueryHistoryResponse>(
    `/query/history/${id}`
  );

  return response.data;
};