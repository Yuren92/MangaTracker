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