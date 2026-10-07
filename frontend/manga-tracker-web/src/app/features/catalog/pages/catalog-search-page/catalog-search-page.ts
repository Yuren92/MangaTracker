import { Component, computed, ElementRef, inject, OnInit, signal, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { getApiErrorMessage } from '../../../../core/http/api-error';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';
import { CollectionsApi } from '../../../collections/services/collections-api';
import { CatalogSearchResult, ComicVineVolumePreview } from '../../models/catalog.models';
import { CatalogApi } from '../../services/catalog-api';

@Component({
  selector: 'app-catalog-search-page',
  imports: [FormsModule, RouterLink, AppAlert],
  templateUrl: './catalog-search-page.html',
  styleUrl: './catalog-search-page.scss'
})
export class CatalogSearchPage implements OnInit {
  @ViewChild('volumePreviewSection')
  private readonly volumePreviewSection?: ElementRef<HTMLElement>;

  private readonly catalogApi = inject(CatalogApi);
  private readonly collectionsApi = inject(CollectionsApi);

  readonly query = signal('');
  readonly results = signal<CatalogSearchResult[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly hasSearched = signal(false);
  readonly selectedVolume = signal<ComicVineVolumePreview | null>(null);
  readonly isPreviewLoading = signal(false);
  readonly previewErrorMessage = signal<string | null>(null);
  readonly isImporting = signal(false);
  readonly importSuccessMessage = signal<string | null>(null);
  readonly importErrorMessage = signal<string | null>(null);
  readonly importedCollectionId = signal<string | null>(null);
  readonly importedComicVineVolumeIds = signal<Set<number>>(new Set<number>());
  readonly importedCollectionIdsByVolumeId = signal<Map<number, string>>(new Map<number, string>());

  readonly isSelectedVolumeImported = computed(() => {
    const volume = this.selectedVolume();
    if (!volume) {
      return false;
    }

    return this.importedComicVineVolumeIds().has(volume.comicVineVolumeId);
  });


  ngOnInit(): void {
    this.loadImportedCollections();
  }

  search(): void {
    const query = this.query().trim();

    if (!query) {
      this.errorMessage.set('Escribe el nombre de una serie.');
      this.results.set([]);
      this.hasSearched.set(false);
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.hasSearched.set(true);

    this.catalogApi.search(query).subscribe({
      next: results => {
        this.results.set(results);
        this.isLoading.set(false);
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido buscar en el catálogo.')
        );

        this.results.set([]);
        this.isLoading.set(false);
      }
    });
  }

  previewVolume(item: CatalogSearchResult): void {
    this.isPreviewLoading.set(true);
    this.previewErrorMessage.set(null);
    this.selectedVolume.set(null);
    this.importSuccessMessage.set(null);
    this.importErrorMessage.set(null);
    this.importedCollectionId.set(null);

    this.catalogApi.previewVolume(item.apiDetailUrl).subscribe({
      next: volume => {
        this.selectedVolume.set(volume);
        this.isPreviewLoading.set(false);
        this.scrollToVolumePreview();
      },
      error: error => {
        this.previewErrorMessage.set(
          getApiErrorMessage(error, 'No se ha podido cargar la vista previa.')
        );

        this.isPreviewLoading.set(false);
      }
    });
  }

  closePreview(): void {
    this.selectedVolume.set(null);
    this.previewErrorMessage.set(null);
  }

  isVolumeImported(item: CatalogSearchResult): boolean {
    return this.importedComicVineVolumeIds().has(item.comicVineVolumeId);
  }

  getImportedCollectionId(item: CatalogSearchResult): string | null {
    return this.importedCollectionIdsByVolumeId().get(item.comicVineVolumeId) ?? null;
  }

  importSelectedVolume(): void {
    const volume = this.selectedVolume();

    if (!volume) {
      return;
    }

    this.isImporting.set(true);
    this.importSuccessMessage.set(null);
    this.importErrorMessage.set(null);

    this.collectionsApi.importComicVineVolume({
      apiDetailUrl: volume.apiDetailUrl
    }).subscribe({
      next: result => {
        this.importedCollectionId.set(result.userCollectionId);

        // A partial import keeps the button enabled so it can be resumed right away.
        if (result.isCompleted) {
          this.importedComicVineVolumeIds.update(current => {
            const next = new Set(current);
            next.add(result.comicVineVolumeId);
            return next;
          });

          this.importedCollectionIdsByVolumeId.update(current => {
            const next = new Map(current);
            next.set(result.comicVineVolumeId, result.userCollectionId);
            return next;
          });
        }

        // A partial import is kept and resumed: importing again only fetches what is missing.
        this.importSuccessMessage.set(
          result.isCompleted
            ? `${result.title} se ha importado con ${result.importedTomes} tomos.`
            : `${result.title} se ha importado a medias (${result.importedTomes} de ${result.totalIssues} tomos). ` +
              'Vuelve a importarla para completar los que faltan; la sincronización también lo hará.'
        );

        this.isImporting.set(false);
      },
      error: error => {
        this.importErrorMessage.set(
          getApiErrorMessage(error, 'No se ha podido importar esta edición.')
        );

        this.isImporting.set(false);
      }
    });
  }

  private scrollToVolumePreview(): void {
    setTimeout(() => {
      this.volumePreviewSection?.nativeElement.scrollIntoView({
        behavior: 'smooth',
        block: 'start'
      });
    });
  }

  private loadImportedCollections(): void {
    this.collectionsApi.getCollections().subscribe({
      next: response => {
        const importedVolumeIds = new Set<number>();
        const collectionIdsByVolumeId = new Map<number, string>();

        for (const collection of response.items) {
          importedVolumeIds.add(collection.comicVineVolumeId);
          collectionIdsByVolumeId.set(collection.comicVineVolumeId, collection.id);
        }

        this.importedComicVineVolumeIds.set(importedVolumeIds);
        this.importedCollectionIdsByVolumeId.set(collectionIdsByVolumeId);
      },
      error: () => {
        this.importedComicVineVolumeIds.set(new Set<number>());
        this.importedCollectionIdsByVolumeId.set(new Map<number, string>());
      }
    });
  }
}