import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { getApiErrorMessage } from '../../../../core/http/api-error';
import { MangaCollectionListItem } from '../../models/collection.models';
import { CollectionApi } from '../../services/collection-api';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';
import { CollectionCard } from '../../components/collection-card/collection-card';


@Component({
  selector: 'app-collection-page',
  imports: [RouterLink, AppAlert, CollectionCard],
  templateUrl: './collection-page.html',
  styleUrl: './collection-page.scss'
})
export class CollectionPage {
  private readonly collectionApi = inject(CollectionApi);

  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly items = signal<MangaCollectionListItem[]>([]);
  readonly removingItemId = signal<string | null>(null);

  constructor() {
    this.loadCollection();
  }

  loadCollection(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.collectionApi.getCollection().subscribe({
      next: response => {
        this.items.set(response.items);
        this.isLoading.set(false);
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido cargar tu colección.')
        );

        this.isLoading.set(false);
      }
    });
  }

  removeFromCollection(item: MangaCollectionListItem): void {
    const shouldRemove = confirm(
      `¿Seguro que quieres quitar "${item.title}" de tu colección?`
    );

    if (!shouldRemove) {
      return;
    }

    this.removingItemId.set(item.id);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.collectionApi.removeMangaFromCollection(item.id).subscribe({
      next: response => {
        this.items.update(currentItems =>
          currentItems.filter(currentItem => currentItem.id !== item.id)
        );

        this.successMessage.set(response.message);
        this.removingItemId.set(null);
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido quitar el manga de tu colección.')
        );

        this.removingItemId.set(null);
      }
    });
  }
}