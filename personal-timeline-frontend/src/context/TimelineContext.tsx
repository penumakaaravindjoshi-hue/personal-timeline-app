import React, { createContext, useState, useEffect, type ReactNode, useCallback } from 'react';
import { type TimelineEntry } from '../types/TimelineEntry';
import { timelineService } from '../services/timelineService';
import { useAuth } from '../hooks/useAuth';

interface TimelineFilters {
    search?: string;
    type?: string;
    category?: string;
}

interface TimelineContextType {
    timelineEntries: TimelineEntry[];
    loading: boolean;
    error: string | null;
    fetchTimelineEntries: (filters?: TimelineFilters) => Promise<void>;
    addEntry: (
        entry: Omit<TimelineEntry, 'id' | 'userId' | 'createdAt' | 'updatedAt'>
    ) => Promise<TimelineEntry | undefined>;
    updateEntry: (id: number, entry: Omit<TimelineEntry, 'userId' | 'createdAt'>) => Promise<TimelineEntry | undefined>;
    deleteEntry: (id: number) => Promise<void>;
}

export const TimelineContext = createContext<TimelineContextType | undefined>(undefined);

interface TimelineProviderProps {
    children: ReactNode;
}

export const TimelineProvider: React.FC<TimelineProviderProps> = ({ children }) => {
    const { token, isAuthenticated } = useAuth();
    const [timelineEntries, setTimelineEntries] = useState<TimelineEntry[]>([]);
    const [loading, setLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);

    const fetchTimelineEntries = useCallback(async (filters?: TimelineFilters) => {
        if (!token) {
            setLoading(false);
            return;
        }
        setLoading(true);
        setError(null);
        try {
            const data = await timelineService.getAllTimelineEntries(token, filters);
            setTimelineEntries(data);
        } catch (err) {
            console.error('Failed to fetch timeline entries:', err);
            setError('Failed to load timeline entries.');
        } finally {
            setLoading(false);
        }
    }, [token]);

    useEffect(() => {
        if (isAuthenticated) {
            fetchTimelineEntries();
        } else {
            setTimelineEntries([]);
            setLoading(false);
        }
    }, [isAuthenticated, fetchTimelineEntries]);

    const addEntry = async (
        entry: Omit<TimelineEntry, 'id' | 'userId' | 'createdAt' | 'updatedAt'>
    ): Promise<TimelineEntry | undefined> => {
        if (!token) {
            setError('Authentication token not available.');
            return undefined;
        }
        try {
            const newEntry = await timelineService.createTimelineEntry(entry, token);
            setTimelineEntries((prev) => [...prev, newEntry]);
            return newEntry;
        } catch (err: any) {
            console.error('Failed to add timeline entry:', err);
            setError(err.response?.data?.message || 'Failed to add entry.');
            return undefined;
        }
    };

    const updateEntry = async (
        id: number,
        entry: Omit<TimelineEntry, 'userId' | 'createdAt'>
    ): Promise<TimelineEntry | undefined> => {
        if (!token) {
            setError('Authentication token not available.');
            return undefined;
        }
        try {
            const updatedEntry = await timelineService.updateTimelineEntry(id, entry, token);
            setTimelineEntries((prev) => prev.map((e) => (e.id === id ? updatedEntry : e)));
            return updatedEntry;
        } catch (err: any) {
            console.error('Failed to update timeline entry:', err);
            setError(err.response?.data?.message || 'Failed to update entry.');
            return undefined;
        }
    };

    const deleteEntry = async (id: number) => {
        if (!token) {
            setError('Authentication token not available.');
            return;
        }
        try {
            await timelineService.deleteTimelineEntry(id, token);
            setTimelineEntries((prev) => prev.filter((e) => e.id !== id));
        } catch (err: any) {
            console.error('Failed to delete timeline entry:', err);
            setError(err.response?.data?.message || 'Failed to delete entry.');
        }
    };

    return (
        <TimelineContext.Provider
            value={{
                timelineEntries,
                loading,
                error,
                fetchTimelineEntries,
                addEntry,
                updateEntry,
                deleteEntry,
            }}
        >
            {children}
        </TimelineContext.Provider>
    );
};
