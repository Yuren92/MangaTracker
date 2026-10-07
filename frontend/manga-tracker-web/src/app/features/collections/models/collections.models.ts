export interface ImportComicVineVolumeRequest {
  apiDetailUrl: string;
}

export interface ImportComicVineVolumeResponse {
  editionId: string;
  userCollectionId: string;
  comicVineVolumeId: number;
  title: string;
  publisherName: string | null;
  totalIssues: number;
  importedTomes: number;
  isCompleted: boolean;
}

export interface UserCollectionSummary {
  id: string;
  editionId: string;
  comicVineVolumeId: number;
  comicVineApiDetailUrl: string;
  title: string;
  publisherName: string | null;
  imageUrl: string | null;
  totalTomes: number;
  ownedTomes: number;
  pendingTomes: number;
}

export interface GetUserCollectionsResponse {
  items: UserCollectionSummary[];
}

export interface UserCollectionDetail {
  id: string;
  editionId: string;
  title: string;
  publisherName: string | null;
  imageUrl: string | null;
  totalTomes: number;
  ownedTomes: number;
  pendingTomes: number;
  tomes: UserCollectionTome[];
}

export interface UserCollectionTome {
  tomeId: string;
  comicVineIssueId: number;
  issueNumber: string;
  normalizedNumber: number | null;
  title: string | null;
  imageUrl: string | null;
  coverDate: string | null;
  storeDate: string | null;
  isOwned: boolean;
}

export interface TomeOwnershipResponse {
  collectionId: string;
  tomeId: string;
  isOwned: boolean;
  totalTomes: number;
  ownedTomes: number;
  pendingTomes: number;
}

export interface MarkAllTomesAsOwnedResponse {
  collectionId: string;
  totalTomes: number;
  ownedTomes: number;
  pendingTomes: number;
}

export interface SyncCollectionsResponse {
  checkedCollections: number;
  syncedCollections: number;
  skippedCollections: number;
  deferredCollections: number;
  failedCollections: number;
  newTomes: number;
}

export interface PendingTomesResponse {
  items: PendingTome[];
}

export interface PendingTome {
  collectionId: string;
  editionId: string;
  tomeId: string;
  seriesTitle: string;
  editionName: string;
  publisherName: string | null;
  issueNumber: string;
  normalizedNumber: number | null;
  tomeTitle: string | null;
  imageUrl: string | null;
  coverDate: string | null;
  storeDate: string | null;
}