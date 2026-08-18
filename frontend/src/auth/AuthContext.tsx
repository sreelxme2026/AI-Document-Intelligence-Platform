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
  isLoggedIn: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [token, setToken] = useState<string | null>(
    isAuthenticated() ? getToken() : null
  );

  const login = async (email: string, password: string) => {
    const result = await loginApi({ email, password });

    saveAuth(result.token, result.expiresAt);
    setToken(result.token);
  };

  const logout = () => {
    clearAuth();
    setToken(null);
  };

  return (
    <AuthContext.Provider
      value={{
        token,
        isLoggedIn: !!token,
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
    throw new Error("useAuth must be used inside AuthProvider");
  }

  return context;
};