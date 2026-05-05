export interface AddMangaToCollectionRequest {
  malId: number;
  customTotalVolumes: number | null;
}

export interface AddMangaToCollectionResponse {
  id: string;
  userId: string;
  malId: number;
  title: string;
  imageUrl: string | null;
  effectiveTotalVolumes: number | null;
}

export interface MangaCollectionListItem {
  id: string;
  malId: number;
  title: string;
  imageUrl: string | null;
  effectiveTotalVolumes: number | null;
  ownedVolumesCount: number;
  missingVolumesCount: number;
}

export interface GetMangaCollectionResponse {
  items: MangaCollectionListItem[];
}

export interface MessageResponse {
  message: string;
}

export interface OwnedVolume {
  volumeNumber: number;
  purchaseDate: string | null;
  price: number | null;
  store: string | null;
}

export interface MangaCollectionDetail {
  id: string;
  userId: string;
  malId: number;
  title: string;
  imageUrl: string | null;
  effectiveTotalVolumes: number | null;
  ownedVolumes: OwnedVolume[];
  missingVolumeNumbers: number[];
}

export interface GetMangaCollectionItemResponse {
  item: MangaCollectionDetail;
}

export interface AddOwnedVolumeRequest {
  volumeNumber: number;
  purchaseDate: string | null;
  price: number | null;
  store: string | null;
}

export interface AddOwnedVolumeResponse {
  collectionItemId: string;
  volumeNumber: number;
  purchaseDate: string | null;
  price: number | null;
  store: string | null;
}

export interface UpdateCustomTotalVolumesRequest {
  totalVolumes: number | null;
}

export interface UpdateCustomTotalVolumesResponse {
  collectionItemId: string;
  effectiveTotalVolumes: number | null;
}