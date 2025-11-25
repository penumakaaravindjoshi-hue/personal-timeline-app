import React, { useState, useEffect, useCallback } from 'react';
import { useApiSync } from '../../hooks/useApiSync';
import { useAuth } from '../../hooks/useAuth';
import { apiService } from '../../services/apiService';
import type { ApiConnectionStatus } from '../../types/ApiConnection';
import { Card, CardHeader, CardBody, Button, addToast } from '@heroui/react'; // Import addToast
import { Icon } from '@iconify/react';

interface AvailableApi {
    provider: string;
    icon: string;
}

// Define the APIs that the frontend knows about
const availableApis: AvailableApi[] = [
    { provider: 'GitHub', icon: 'mdi:github' },
    { provider: 'Notion', icon: 'mdi:notion' },
];

export const ApiConnections: React.FC = () => {
    const { token } = useAuth();
    const [connections, setConnections] = useState<ApiConnectionStatus[]>([]);
    const [loading, setLoading] = useState(true);
    const [isSyncing, setIsSyncing] = useState<Record<string, boolean>>({}); // State to track syncing status per provider

    const fetchConnections = useCallback(async () => {
        if (!token) {
            setLoading(false);
            return;
        }
        try {
            setLoading(true);
            const fetchedConnections = await apiService.getApiConnections(token);
            setConnections(fetchedConnections);
        } catch (error) {
            console.error('Error fetching API connections:', error);
            addToast({
                title: 'Error',
                description: 'Failed to fetch API connections.',
            });
        } finally {
            setLoading(false);
        }
    }, [token]);

    const handleSyncComplete = useCallback(
        (provider: string, success: boolean) => {
            setIsSyncing((prev) => ({ ...prev, [provider]: false }));
            if (success) {
                addToast({
                    title: 'Sync Successful',
                    description: `${provider} data synced successfully!`,
                });
                fetchConnections(); // Refresh connections list to update LastSyncAt
            } else {
                addToast({
                    title: 'Sync Failed',
                    description: `Failed to sync ${provider} data. Check console for details.`,
                });
            }
        },
        [fetchConnections]
    );

    const { connectApi, disconnectApi, syncApi } = useApiSync(handleSyncComplete, setIsSyncing); // Pass handleSyncComplete and setIsSyncing

    useEffect(() => {
        fetchConnections();
    }, [fetchConnections]);

    const getConnectionDetails = (provider: string) => {
        const connection = connections?.find((c) => c.apiProvider.toLowerCase() === provider.toLowerCase());
        return {
            status: connection && connection.isActive ? 'Connected' : 'Disconnected',
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            //@ts-expect-error
            lastSyncAt: connection?.lastSyncAt ? new Date(connection.lastSyncAt).toLocaleString() : 'N/A',
        };
    };

    if (loading) {
        return <div className="container mx-auto p-4">Loading connections...</div>;
    }

    return (
        <div className="container mx-auto p-4">
            <h1 className="text-3xl font-light mb-6 text-gray-800">API Connections</h1>

            <p className="mb-4 text-gray-600 font-light">
                Connect your favorite services to automatically populate your timeline.
            </p>

            <Card className="shadow-sm border border-gray-200 rounded-lg p-3">
                <CardHeader>
                    <h2 className="text-xl mb-2 text-gray-700">Manage Connections</h2>
                </CardHeader>
                <CardBody>
                    <ul className="space-y-2">
                        {availableApis.map((api) => {
                            const { status, lastSyncAt } = getConnectionDetails(api.provider);
                            const syncing = isSyncing[api.provider] || false;
                            return (
                                <li
                                    key={api.provider}
                                    className="flex justify-between items-center border-b pb-2 last:border-b-0 last:pb-0 border-gray-100"
                                >
                                    <div className="flex items-center gap-3">
                                        <Icon icon={api.icon} width={24} />
                                        <div>
                                            <p className="text-lg font-light text-gray-700">{api.provider}</p>
                                            <p
                                                className={`text-sm ${
                                                    status === 'Connected' ? 'text-green-600' : 'text-red-600'
                                                } font-light`}
                                            >
                                                Status: {status}{' '}
                                                {status === 'Connected' && `(Last Sync: ${lastSyncAt})`}
                                            </p>
                                        </div>
                                    </div>
                                    <div className="space-x-2">
                                        {status === 'Connected' ? (
                                            <>
                                                <Button
                                                    onPress={() => syncApi(api.provider)}
                                                    color="primary"
                                                    size="sm"
                                                    radius="lg"
                                                    isDisabled={syncing} // Disable while syncing
                                                    startContent={
                                                        syncing ? (
                                                            <Icon icon="mdi:sync" className="animate-spin" />
                                                        ) : null
                                                    }
                                                >
                                                    {syncing ? 'Syncing...' : 'Sync Now'}
                                                </Button>
                                                <Button
                                                    onPress={() => disconnectApi(api.provider)}
                                                    color="danger"
                                                    size="sm"
                                                    radius="lg"
                                                    isDisabled={syncing} // Disable while syncing
                                                >
                                                    Disconnect
                                                </Button>
                                            </>
                                        ) : (
                                            <Button
                                                onPress={() => connectApi(api.provider)}
                                                color="primary"
                                                size="sm"
                                                radius="lg"
                                                isDisabled={syncing} // Disable while syncing
                                            >
                                                Connect
                                            </Button>
                                        )}
                                    </div>
                                </li>
                            );
                        })}
                    </ul>
                </CardBody>
            </Card>
        </div>
    );
};
