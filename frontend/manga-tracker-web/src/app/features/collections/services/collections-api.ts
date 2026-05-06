import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../../environments/environment';
import {
  GetUserCollectionsResponse,
  ImportComicVineVolumeRequest,
  ImportComicVineVolumeResponse,
  PendingTomesResponse,
  TomeOwnershipResponse,
  MarkAllTomesAsOwnedResponse,
  UserCollectionDetail
} from '../models/collections.models';

@Injectable({
  providedIn: 'root'
})
export class CollectionsApi {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  importComicVineVolume(request: ImportComicVineVolumeRequest) {
    return this.http.post<ImportComicVineVolumeResponse>(
      `${this.apiUrl}/api/collections/import-comic-vine-volume`,
      request
    );
  }

  getCollections() {
    return this.http.get<GetUserCollectionsResponse>(
      `${this.apiUrl}/api/collections`
    );
  }

  getCollectionDetail(collectionId: string) {
    return this.http.get<UserCollectionDetail>(
      `${this.apiUrl}/api/collections/${collectionId}`
    );
  }

  getPendingTomes() {
    return this.http.get<PendingTomesResponse>(
      `${this.apiUrl}/api/collections/pending-tomes`
    );
  }

  markTomeAsOwned(collectionId: string, tomeId: string) {
    return this.http.post<TomeOwnershipResponse>(
      `${this.apiUrl}/api/collections/${collectionId}/tomes/${tomeId}/owned`,
      {}
    );
  }

  unmarkTomeAsOwned(collectionId: string, tomeId: string) {
    return this.http.delete<TomeOwnershipResponse>(
      `${this.apiUrl}/api/collections/${collectionId}/tomes/${tomeId}/owned`
    );
  }

  markAllTomesAsOwned(collectionId: string) {
    return this.http.post<MarkAllTomesAsOwnedResponse>(
      `${this.apiUrl}/api/collections/${collectionId}/tomes/owned-all`,
      {}
    );
  }
}