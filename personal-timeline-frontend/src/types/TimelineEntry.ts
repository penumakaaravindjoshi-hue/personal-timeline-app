export interface TimelineEntry {
  id: number;
  userId: number;
  title: string;
  description: string | null;
  eventDate: string; // ISO date string
  entryType: 'Achievement' | 'Activity' | 'Milestone' | 'Memory';
  category: string | null;
  imageUrl: string | null;
  externalUrl: string | null;
  sourceApi: string | null;
  externalId: string | null;
  metadata: string | null; // JSON string
  createdAt: string; // ISO date string
  updatedAt: string; // ISO date string
}
