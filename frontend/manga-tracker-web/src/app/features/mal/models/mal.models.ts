export interface MalMangaSearchResult {
  malId: number;
  title: string;
  imageUrl: string | null;
}

export interface SearchMalMangaResponse {
  items: MalMangaSearchResult[];
}

export interface MalMangaRecommendation {
  malId: number;
  title: string;
  imageUrl: string | null;
  numRecommendations: number;
}

export interface MalMangaDetail {
  malId: number;
  title: string;
  imageUrl: string | null;
  totalVolumes: number | null;
  totalChapters: number | null;
  status: string | null;
  synopsis: string | null;
  recommendations: MalMangaRecommendation[];
  isInCollection: boolean;
  collectionItemId: string | null;
}

export interface GetMalMangaDetailResponse {
  item: MalMangaDetail;
}