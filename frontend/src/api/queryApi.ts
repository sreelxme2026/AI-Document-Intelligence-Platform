import api from "./client";
import type {
  RagRequest,
  RagResult,
} from "../types/api";

export const askQuestion = async (
  data: RagRequest
): Promise<RagResult> => {
  const response = await api.post<RagResult>(
    "/query",
    data
  );

  return response.data;
};