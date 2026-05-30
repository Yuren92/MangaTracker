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
}