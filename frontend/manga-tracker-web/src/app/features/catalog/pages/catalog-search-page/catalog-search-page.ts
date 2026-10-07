import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, ElementRef, inject, OnInit, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { getApiErrorMessage } from '../../../../core/http/api-error';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';
import { TomesPipe } from '../../../../shared/pipes/tomes.pipe';
import { CollectionsApi } from '../../../collections/services/collections-api';
import { CatalogSearchResult, ComicVineVolumePreview } from '../../models/catalog.models';
import { CatalogApi } from '../../services/catalog-api';

@Component({
  selector: 'app-catalog-search-page',
  imports: [FormsModule, RouterLink, NgTemplateOutlet, AppAlert, TomesPipe],
  templateUrl: './catalog-search-page.html',
  styleUrl: './catalog-search-page.scss'
})
export class CatalogSearchPage implements OnInit {
  private readonly catalogApi = inject(CatalogApi);
  private readonly collectionsApi = inject(CollectionsApi);
  private readonly previewDialog = viewChild.required<ElementRef<HTMLDialogElement>>('previewDialog');

  readonly query = signal('');
  readonly results = signal<CatalogSearchResult[]>([]);
  readonly searchedFor = signal<string | null>(null);
  readonly isSearching = signal(false);
  readonly searchError = signal<string | null>(null);

  readonly preview = signal<ComicVineVolumePreview | null>(null);
  readonly previewItem = signal<CatalogSearchResult | null>(null);
  readonly isPreviewLoading = signal(false);
  readonly previewError = signal<string | null>(null);

  readonly isAdding = signal(false);
  readonly addResult = signal<{ collectionId: string; message: string } | null>(null);
  readonly addError = signal<string | null>(null);

  // Volumes already on the user's shelf, by Comic Vine volume id -> collection id.
  readonly shelf = signal<ReadonlyMap<number, string>>(new Map<number, string>());

  readonly previewCollectionId = computed(() => {
    const item = this.previewItem();
    return item ? this.shelf().get(item.comicVineVolumeId) ?? null : null;
  });

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

  openPreview(item: CatalogSearchResult): void {
    this.previewItem.set(item);
    this.preview.set(null);
    this.previewError.set(null);
    this.addResult.set(null);
    this.addError.set(null);
    this.isPreviewLoading.set(true);
    this.previewDialog().nativeElement.showModal();

    this.catalogApi.previewVolume(item.apiDetailUrl).subscribe({
      next: volume => {
        this.preview.set(volume);
        this.isPreviewLoading.set(false);
      },
      error: error => {
        this.previewError.set(getApiErrorMessage(error, 'No se ha podido cargar esta edición.'));
        this.isPreviewLoading.set(false);
      }
    });
  }

  closePreview(): void {
    this.previewDialog().nativeElement.close();
  }

  addToShelf(): void {
    const volume = this.preview();

    if (!volume) {
      return;
    }

    this.isAdding.set(true);
    this.addError.set(null);

    this.collectionsApi.importComicVineVolume({ apiDetailUrl: volume.apiDetailUrl }).subscribe({
      next: result => {
        this.isAdding.set(false);

        // A partial import is kept and resumed later, so the series is on the shelf
        // either way; the message says whether some tomes are still on their way.
        this.shelf.update(current => new Map(current).set(result.comicVineVolumeId, result.userCollectionId));
        this.addResult.set({
          collectionId: result.userCollectionId,
          message: result.isCompleted
            ? `Añadida con sus ${result.importedTomes} tomos.`
            : `Añadida con ${result.importedTomes} de ${result.totalIssues} tomos; el resto llegará en la próxima actualización.`
        });
      },
      error: error => {
        this.addError.set(getApiErrorMessage(error, 'No se ha podido añadir esta edición.'));
        this.isAdding.set(false);
      }
    });
  }
}
