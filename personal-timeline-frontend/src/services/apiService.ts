import axios from 'axios';
import type { ApiConnectionStatus } from '../types/ApiConnection';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7027';

const getAuthHeaders = (token: string) => ({
    headers: {
        Authorization: `Bearer ${token}`,
    },
});

export const apiService = {
    getApiConnections: async (token: string): Promise<ApiConnectionStatus[]> => {
        const response = await axios.get<ApiConnectionStatus[]>(`${API_BASE_URL}/api/connections`, getAuthHeaders(token));
        return response.data;
    },

    connectApi: (apiProvider: string) => {
        // Redirect the user to the backend's OAuth initiation endpoint for the specific provider
        window.location.href = `${API_BASE_URL}/auth/${apiProvider.toLowerCase()}`;
    },

    disconnectApi: async (apiProvider: string, token: string): Promise<void> => {
        await axios.delete(`${API_BASE_URL}/api/connections/${apiProvider.toLowerCase()}`, getAuthHeaders(token));
    },

    syncApi: async (apiProvider: string, token: string): Promise<any[]> => {
        const response = await axios.post<any[]>(`${API_BASE_URL}/api/connections/${apiProvider.toLowerCase()}/sync`, {}, getAuthHeaders(token));
        return response.data;
    },
};
