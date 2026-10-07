import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { UserCollectionSummary } from '../../models/collections.models';
import { CollectionsApi } from '../../services/collections-api';
import { getApiErrorMessage } from '../../../../core/http/api-error';

@Component({
  selector: 'app-user-collections-page',
  imports: [RouterLink],
  templateUrl: './user-collections-page.html',
  styleUrl: './user-collections-page.scss'
})
export class UserCollectionsPage implements OnInit {
  private readonly collectionsApi = inject(CollectionsApi);

  readonly collections = signal<UserCollectionSummary[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly deletingCollectionIds = signal<Set<string>>(new Set<string>());

  readonly isSyncing = signal(false);
  readonly syncMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadCollections();
  }

  private loadCollections(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.collectionsApi.getCollections().subscribe({
      next: response => {
        this.collections.set(response.items);
        this.isLoading.set(false);

        // The list shows local data at once; new tomes from Comic Vine arrive after the sync.
        if (response.items.length > 0) {
          this.syncCollections();
        }
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se han podido cargar tus colecciones.')
        );

        this.collections.set([]);
        this.isLoading.set(false);
      }
    });
  }

  deleteCollection(collectionId: string, title: string): void {
    const confirmed = window.confirm(
      `¿Seguro que quieres eliminar "${title}" de tus colecciones?`
    );

    if (!confirmed) {
      return;
    }

    this.setCollectionDeleting(collectionId, true);
    this.errorMessage.set(null);

    this.collectionsApi.deleteCollection(collectionId).subscribe({
      next: () => {
        this.collections.update(collections =>
          collections.filter(collection => collection.id !== collectionId)
        );

        this.setCollectionDeleting(collectionId, false);
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido eliminar la colección.')
        );

        this.setCollectionDeleting(collectionId, false);
      }
    });
  }

  private setCollectionDeleting(collectionId: string, isDeleting: boolean): void {
    this.deletingCollectionIds.update(current => {
      const next = new Set(current);

      if (isDeleting) {
        next.add(collectionId);
      } else {
        next.delete(collectionId);
      }

      return next;
    });
  }

  private syncCollections(): void {
    if (this.isSyncing()) {
      return;
    }

    this.isSyncing.set(true);
    this.syncMessage.set('Actualizando colecciones...');

    this.collectionsApi.syncCollections().subscribe({
      next: () => {
        this.isSyncing.set(false);
        this.syncMessage.set(null);
        this.loadCollectionsAfterSync();
      },
      error: () => {
        this.isSyncing.set(false);
        this.syncMessage.set(null);
      }
    });
  }

  private loadCollectionsAfterSync(): void {
    this.collectionsApi.getCollections().subscribe({
      next: response => {
        this.collections.set(response.items);
      },
      error: () => {
        // If the reload after the sync fails, the list already on screen stays.
      }
    });
  }
}