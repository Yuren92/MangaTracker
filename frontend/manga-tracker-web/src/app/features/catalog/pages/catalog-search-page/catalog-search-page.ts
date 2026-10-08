import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { getApiErrorMessage } from '../../../../core/http/api-error';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';
import { CoverPipe } from '../../../../shared/pipes/cover.pipe';
import { TomesPipe } from '../../../../shared/pipes/tomes.pipe';
import { CollectionsApi } from '../../../collections/services/collections-api';
import { CatalogSearchResult } from '../../models/catalog.models';
import { CatalogApi } from '../../services/catalog-api';

// What a result card is doing right now. Each card has its own, so several series can
// be added at once and the rest of the page keeps working while they are.
export type AddState =
  | { status: 'adding' }
  | { status: 'error'; message: string };

@Component({
  selector: 'app-catalog-search-page',
  imports: [FormsModule, RouterLink, AppAlert, CoverPipe, TomesPipe],
  templateUrl: './catalog-search-page.html',
  styleUrl: './catalog-search-page.scss'
})
export class CatalogSearchPage implements OnInit {
  private readonly catalogApi = inject(CatalogApi);
  private readonly collectionsApi = inject(CollectionsApi);

  readonly query = signal('');
  readonly results = signal<CatalogSearchResult[]>([]);
  readonly searchedFor = signal<string | null>(null);
  readonly isSearching = signal(false);
  readonly searchError = signal<string | null>(null);

  // Volumes already on the user's shelf, by Comic Vine volume id -> collection id.
  readonly shelf = signal<ReadonlyMap<number, string>>(new Map<number, string>());

  // In-flight adds and failed ones, by Comic Vine volume id.
  readonly adds = signal<ReadonlyMap<number, AddState>>(new Map<number, AddState>());

  // Series added on this visit whose tomes the server is still downloading.
  readonly downloading = signal<ReadonlySet<number>>(new Set<number>());

  // Read out by screen readers: the button that was pressed turns into a link, so
  // without this nothing would say the series was added.
  readonly announcement = signal('');

  ngOnInit(): void {
    this.collectionsApi.getCollections().subscribe({
      next: response => this.shelf.set(
        new Map(response.items.map(collection => [collection.comicVineVolumeId, collection.id]))),
      // Without it the page still works; it just cannot flag series already on the shelf.
      error: () => undefined
    });
  }

  search(): void {
    const query = this.query().trim();

    if (!query) {
      this.searchError.set('Escribe el nombre de una serie.');
      return;
    }

    this.isSearching.set(true);
    this.searchError.set(null);

    this.catalogApi.search(query, 20).subscribe({
      next: results => {
        this.results.set(results);
        this.searchedFor.set(query);
        this.isSearching.set(false);
      },
      error: error => {
        this.searchError.set(getApiErrorMessage(error, 'No se ha podido buscar en el catálogo.'));
        this.results.set([]);
        this.isSearching.set(false);
      }
    });
  }

  collectionIdFor(item: CatalogSearchResult): string | null {
    return this.shelf().get(item.comicVineVolumeId) ?? null;
  }

  addStateFor(item: CatalogSearchResult): AddState | null {
    return this.adds().get(item.comicVineVolumeId) ?? null;
  }

  addErrorFor(item: CatalogSearchResult): string | null {
    const state = this.addStateFor(item);
    return state?.status === 'error' ? state.message : null;
  }

  isDownloading(item: CatalogSearchResult): boolean {
    return this.downloading().has(item.comicVineVolumeId);
  }

  // The server stores the series straight away and downloads its tomes in the
  // background, so this answers in about a second whatever the size of the series.
  add(item: CatalogSearchResult): void {
    const volumeId = item.comicVineVolumeId;

    if (this.addStateFor(item)?.status === 'adding' || this.collectionIdFor(item)) {
      return;
    }

    this.setAddState(volumeId, { status: 'adding' });

    this.collectionsApi.importComicVineVolume({ apiDetailUrl: item.apiDetailUrl }).subscribe({
      next: result => {
        this.setAddState(volumeId, null);
        this.shelf.update(current => new Map(current).set(volumeId, result.userCollectionId));

        if (result.tomesPending) {
          this.downloading.update(current => new Set(current).add(volumeId));
        }

        this.announcement.set(`«${item.name}» añadida a tu estantería.`);
      },
      error: error => this.setAddState(volumeId, {
        status: 'error',
        message: getApiErrorMessage(error, 'No se ha podido añadir.')
      })
    });
  }

  private setAddState(volumeId: number, state: AddState | null): void {
    this.adds.update(current => {
      const next = new Map(current);

      if (state) {
        next.set(volumeId, state);
      } else {
        next.delete(volumeId);
      }

      return next;
    });
  }
}
