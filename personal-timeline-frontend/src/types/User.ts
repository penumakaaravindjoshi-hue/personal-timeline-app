import type { TimelineEntry } from "./TimelineEntry";
import type { ApiConnection } from "./ApiConnection";

export interface User {
  id: number;
  oAuthProvider: string;
  oAuthId: string;
  email: string;
  displayName: string;
  profileImageUrl: string;
  createdAt: string; // ISO date string
  lastLoginAt: string; // ISO date string
  timelineEntries?: TimelineEntry[]; // Optional, as it might not always be loaded
  apiConnections?: ApiConnection[]; // Optional
}

export interface AuthResponse {
  user: User;
  token: string;
}
