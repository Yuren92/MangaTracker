export interface CatalogSearchResult {
  comicVineVolumeId: number;
  name: string;
  publisherName: string | null;
  countOfIssues: number | null;
  imageUrl: string | null;
  startYear: number | null;
  deck: string | null;
  siteDetailUrl: string | null;
  apiDetailUrl: string;
}
