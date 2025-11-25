import axios from 'axios';
import type { AuthResponse } from '../types/User';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7027';

export const authService = {
    loginWithGoogle: () => {
        // Redirect the user to the backend's Google OAuth initiation endpoint
        window.location.href = `${API_BASE_URL}/auth/google`;
    },

    getAuthMe: async (): Promise<AuthResponse> => {
        // This endpoint is called by the frontend after the Google OAuth redirect.
        // The backend should have set a session cookie, allowing this request to succeed.
        const response = await axios.get<AuthResponse>(`${API_BASE_URL}/auth/me`, {
            withCredentials: true, // Important for sending cookies
        });
        return response.data;
    },

    logout: async (token: string): Promise<void> => {
        // Invalidate the session on the backend
        await axios.post(
            `${API_BASE_URL}/auth/logout`,
            {},
            {
                headers: {
                    Authorization: `Bearer ${token}`,
                },
                withCredentials: true, // Important for sending cookies
            }
        );
    },
};
