// src/hooks/useApiSync.ts
import { useAuth } from './useAuth';
import { apiService } from '../services/apiService';
import { useCallback } from 'react';

type SetIsSyncing = React.Dispatch<React.SetStateAction<Record<string, boolean>>>;

export const useApiSync = (onSyncComplete?: (provider: string, success: boolean) => void, setIsSyncing?: SetIsSyncing) => {
    const { token } = useAuth();

    const connectApi = useCallback((provider: string) => {
        apiService.connectApi(provider);
    }, []);

    const disconnectApi = useCallback(async (provider: string) => {
        if (!token) {
            console.error("No token available for disconnecting API");
            return;
        }
        try {
            if (setIsSyncing) setIsSyncing((prev) => ({ ...prev, [provider]: true }));
            await apiService.disconnectApi(provider, token);
            if (onSyncComplete) {
                onSyncComplete(provider, true);
            }
        } catch (error) {
            console.error(`Error disconnecting ${provider}:`, error);
            if (onSyncComplete) {
                onSyncComplete(provider, false);
            }
        } finally {
            if (setIsSyncing) setIsSyncing((prev) => ({ ...prev, [provider]: false }));
        }
    }, [token, onSyncComplete, setIsSyncing]);

    const syncApi = useCallback(async (provider: string) => {
        if (!token) {
            console.error("No token available for syncing API");
            return;
        }
        try {
            if (setIsSyncing) setIsSyncing((prev) => ({ ...prev, [provider]: true }));
            console.log(`Syncing ${provider}...`);
            await apiService.syncApi(provider, token);
            console.log(`${provider} synced successfully.`);
            if (onSyncComplete) {
                onSyncComplete(provider, true);
            }
        } catch (error) {
            console.error(`Error syncing ${provider}:`, error);
            if (onSyncComplete) {
                onSyncComplete(provider, false);
            }
        } finally {
            if (setIsSyncing) setIsSyncing((prev) => ({ ...prev, [provider]: false }));
        }
    }, [token, onSyncComplete, setIsSyncing]);

    return { connectApi, disconnectApi, syncApi };
};