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

export interface ComicVineVolumePreview {
  comicVineVolumeId: number;
  name: string;
  publisherName: string | null;
  countOfIssues: number | null;
  imageUrl: string | null;
  startYear: number | null;
  description: string | null;
  siteDetailUrl: string | null;
  apiDetailUrl: string;
  issues: ComicVineIssuePreview[];
}

export interface ComicVineIssuePreview {
  comicVineIssueId: number;
  issueNumber: string;
  normalizedNumber: number | null;
  title: string | null;
  apiDetailUrl: string;
}