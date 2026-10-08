import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../../environments/environment';
import { CatalogSearchResult } from '../models/catalog.models';

@Injectable({
  providedIn: 'root'
})
export class CatalogApi {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  search(query: string, limit = 10) {
    const params = new HttpParams()
      .set('query', query)
      .set('limit', limit);

    return this.http.get<CatalogSearchResult[]>(
      `${this.apiUrl}/api/catalog/search`,
      { params }
    );
  }
}
