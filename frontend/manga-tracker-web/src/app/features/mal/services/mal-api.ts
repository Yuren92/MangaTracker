import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../../environments/environment';
import {
  GetMalMangaDetailResponse,
  SearchMalMangaResponse
} from '../models/mal.models';

@Injectable({
  providedIn: 'root'
})
export class MalApi {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  searchManga(query: string, limit = 10) {
    const params = new HttpParams()
      .set('query', query)
      .set('limit', limit);

    return this.http.get<SearchMalMangaResponse>(
      `${this.apiUrl}/api/mal/manga/search`,
      { params }
    );
  }

  getMangaDetail(malId: number) {
    return this.http.get<GetMalMangaDetailResponse>(
      `${this.apiUrl}/api/mal/manga/${malId}`
    );
  }
}