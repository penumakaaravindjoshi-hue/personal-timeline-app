export interface ApiConnection {
  id: number;
  userId: number;
  apiProvider: string;
  isActive: boolean;
  lastSyncAt: string | null;
  // Other fields like accessToken are sensitive and should not be sent to the frontend
}

export interface ApiConnectionStatus {
  apiProvider: string;
  isActive: boolean;
}