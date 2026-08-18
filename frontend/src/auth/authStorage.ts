const TOKEN_KEY = "ai_document_token";
const EXPIRY_KEY = "ai_document_token_expiry";

export const saveAuth = (token: string, expiresAt: string) => {
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(EXPIRY_KEY, expiresAt);
};

export const getToken = () => localStorage.getItem(TOKEN_KEY);

export const clearAuth = () => {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(EXPIRY_KEY);
};

export const isAuthenticated = () => {
  const token = getToken();
  const expiry = localStorage.getItem(EXPIRY_KEY);

  if (!token || !expiry) return false;

  if (new Date(expiry).getTime() <= Date.now()) {
    clearAuth();
    return false;
  }

  return true;
};