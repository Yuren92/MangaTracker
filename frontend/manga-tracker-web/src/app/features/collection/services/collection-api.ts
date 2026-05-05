import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../../environments/environment';
import {
  AddMangaToCollectionRequest,
  AddMangaToCollectionResponse,
  AddOwnedVolumeRequest,
  AddOwnedVolumeResponse,
  GetMangaCollectionItemResponse,
  GetMangaCollectionResponse,
  MessageResponse,
  UpdateCustomTotalVolumesRequest,
  UpdateCustomTotalVolumesResponse
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

  removeMangaFromCollection(collectionItemId: string) {
    return this.http.delete<MessageResponse>(
      `${this.apiUrl}/api/collection/${collectionItemId}`
    );
  }

  getCollectionItem(collectionItemId: string) {
  return this.http.get<GetMangaCollectionItemResponse>(
    `${this.apiUrl}/api/collection/${collectionItemId}`
  );
}

addOwnedVolume(collectionItemId: string, request: AddOwnedVolumeRequest) {
  return this.http.post<AddOwnedVolumeResponse>(
    `${this.apiUrl}/api/collection/${collectionItemId}/volumes`,
    request
  );
}

removeOwnedVolume(collectionItemId: string, volumeNumber: number) {
  return this.http.delete<MessageResponse>(
    `${this.apiUrl}/api/collection/${collectionItemId}/volumes/${volumeNumber}`
  );
}

updateCustomTotalVolumes(
  collectionItemId: string,
  request: UpdateCustomTotalVolumesRequest) {
  return this.http.put<UpdateCustomTotalVolumesResponse>(
    `${this.apiUrl}/api/collection/${collectionItemId}/total-volumes`,
    request
  );
}
}