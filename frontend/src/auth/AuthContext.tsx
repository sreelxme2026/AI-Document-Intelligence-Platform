import {
  createContext,
  useContext,
  useState,
  type ReactNode,
} from "react";

import { login as loginApi } from "../api/authApi";

import {
  clearAuth,
  getToken,
  isAuthenticated,
  saveAuth,
} from "./authStorage";

interface AuthContextType {
  token: string | null;
  role: string | null;
  isLoggedIn: boolean;
  isAdmin: boolean;
  login: (email: string, password: string) => Promise<string | null>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

/*
 * Reads the role from the JWT payload.
 *
 * ASP.NET Core can serialize role claims using either:
 *
 * "role"
 *
 * or:
 *
 * "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
 */
const getRoleFromToken = (token: string | null): string | null => {
  if (!token) {
    return null;
  }

  try {
    const parts = token.split(".");

    if (parts.length !== 3) {
      return null;
    }

    const base64Url = parts[1];

    const base64 = base64Url
      .replace(/-/g, "+")
      .replace(/_/g, "/");

    const padded = base64.padEnd(
      base64.length + ((4 - (base64.length % 4)) % 4),
      "="
    );

    const jsonPayload = decodeURIComponent(
      atob(padded)
        .split("")
        .map(
          (char) =>
            "%" +
            ("00" + char.charCodeAt(0).toString(16)).slice(-2)
        )
        .join("")
    );

    const payload = JSON.parse(jsonPayload);

    return (
      payload.role ??
      payload[
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
      ] ??
      null
    );
  } catch {
    return null;
  }
};

export const AuthProvider = ({
  children,
}: {
  children: ReactNode;
}) => {
  const initialToken = isAuthenticated()
    ? getToken()
    : null;

  const [token, setToken] = useState<string | null>(
    initialToken
  );

  const [role, setRole] = useState<string | null>(
    getRoleFromToken(initialToken)
  );

  const login = async (
    email: string,
    password: string
  ): Promise<string | null> => {
    const result = await loginApi({
      email,
      password,
    });

    saveAuth(
      result.token,
      result.expiresAt
    );

    const detectedRole = getRoleFromToken(
      result.token
    );

    setToken(result.token);
    setRole(detectedRole);

    return detectedRole;
  };

  const logout = () => {
    clearAuth();
    setToken(null);
    setRole(null);
  };

  return (
    <AuthContext.Provider
      value={{
        token,
        role,
        isLoggedIn: !!token,
        isAdmin: role === "Admin",
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error(
      "useAuth must be used inside AuthProvider"
    );
  }

  return context;
};