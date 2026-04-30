import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../../environments/environment';
import {
  AddMangaToCollectionRequest,
  AddMangaToCollectionResponse,
  GetMangaCollectionResponse
} from '../models/collection.models';

@Injectable({
  providedIn: 'root'
})
export class CollectionApi {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getCollection() {
    return this.http.get<GetMangaCollectionResponse>(
      `${this.apiUrl}/api/collection`
    );
  }

  addMangaToCollection(request: AddMangaToCollectionRequest) {
    return this.http.post<AddMangaToCollectionResponse>(
      `${this.apiUrl}/api/collection`,
      request
    );
  }
}