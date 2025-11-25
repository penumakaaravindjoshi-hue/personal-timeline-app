import React, {
  createContext,
  useState,
  useEffect,
  type ReactNode,
} from "react";
import type { User } from "../types/User";
import { useLocalStorage } from "../hooks/useLocalStorage";
import { authService } from "../services/authService";

interface AuthContextType {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  login: () => void;
  logout: () => Promise<void>;
  setAuthData: (user: User, token: string) => void;
  loading: boolean;
}

export const AuthContext = createContext<AuthContextType | undefined>(
  undefined
);

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [token, setToken] = useLocalStorage<string | null>("jwt_token", null);
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState<boolean>(true);

  const isAuthenticated = !!token && !!user;

  useEffect(() => {
    const loadUserFromToken = async () => {
      if (token && !user) { // Only fetch if token exists and user is not yet loaded
        try {
          const authResponse = await authService.getAuthMe();
          setUser(authResponse.user);
        } catch (error) {
          console.error("Error loading user from token:", error);
          setToken(null); // Clear invalid token
          setUser(null);
        } finally {
          setLoading(false);
        }
      } else if (!token) { // If no token, ensure loading is false
        setLoading(false);
      }
    };
    loadUserFromToken();
  }, [token, user, setToken]); // Add user to dependencies

  const login = () => {
    authService.loginWithGoogle();
  };

  const logout = async () => {
    if (token) {
      try {
        await authService.logout(token);
      } catch (error) {
        console.error("Error during logout:", error);
      } finally {
        setToken(null);
        setUser(null);
      }
    }
  };

  const setAuthData = (userData: User, jwtToken: string) => {
    setUser(userData);
    setToken(jwtToken);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isAuthenticated,
        login,
        logout,
        setAuthData,
        loading,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

