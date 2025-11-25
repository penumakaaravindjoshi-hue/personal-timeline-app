import axios from 'axios';
import type { TimelineEntry } from '../types/TimelineEntry';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7027';

const getAuthHeaders = (token: string) => ({
    headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json', // Explicitly set Content-Type for JSON bodies
    },
});

interface TimelineFilters {
    search?: string;
    type?: string;
    category?: string;
}

export const timelineService = {
    getAllTimelineEntries: async (token: string, filters?: TimelineFilters): Promise<TimelineEntry[]> => {
        const params = new URLSearchParams();
        if (filters?.search) {
            params.append('search', filters.search);
        }
        if (filters?.type) {
            params.append('type', filters.type);
        }
        if (filters?.category) {
            params.append('category', filters.category);
        }

        const queryString = params.toString();
        const url = `${API_BASE_URL}/timeline${queryString ? `?${queryString}` : ''}`;

        const response = await axios.get<TimelineEntry[]>(url, getAuthHeaders(token));
        return response.data;
    },

    getTimelineEntryById: async (id: number, token: string): Promise<TimelineEntry> => {
        const response = await axios.get<TimelineEntry>(`${API_BASE_URL}/timeline/${id}`, getAuthHeaders(token));
        return response.data;
    },

    createTimelineEntry: async (
        entry: Omit<TimelineEntry, 'id' | 'userId' | 'createdAt' | 'updatedAt'>,
        token: string
    ): Promise<TimelineEntry> => {
        console.log('Creating timeline entry:', entry);
        const response = await axios.post<TimelineEntry>(`${API_BASE_URL}/timeline`, entry, getAuthHeaders(token));
        return response.data;
    },

    updateTimelineEntry: async (
        id: number,
        entry: Omit<TimelineEntry, 'userId' | 'createdAt'>,
        token: string
    ): Promise<TimelineEntry> => {
        const response = await axios.put<TimelineEntry>(`${API_BASE_URL}/timeline/${id}`, entry, getAuthHeaders(token));
        return response.data;
    },

    deleteTimelineEntry: async (id: number, token: string): Promise<void> => {
        await axios.delete(`${API_BASE_URL}/timeline/${id}`, getAuthHeaders(token));
    },
};
