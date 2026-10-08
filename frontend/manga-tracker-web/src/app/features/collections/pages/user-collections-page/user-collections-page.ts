import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { getApiErrorMessage } from '../../../../core/http/api-error';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';
import { TomesPipe } from '../../../../shared/pipes/tomes.pipe';
import { UserCollectionSummary } from '../../models/collections.models';
import { CollectionsApi } from '../../services/collections-api';

@Component({
  selector: 'app-user-collections-page',
  imports: [RouterLink, AppAlert, TomesPipe],
  templateUrl: './user-collections-page.html',
  styleUrl: './user-collections-page.scss'
})
export class UserCollectionsPage implements OnInit {
  private readonly collectionsApi = inject(CollectionsApi);

  readonly collections = signal<UserCollectionSummary[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly totals = computed(() => {
    const collections = this.collections();

    return {
      series: collections.length,
      owned: collections.reduce((sum, collection) => sum + collection.ownedTomes, 0),
      total: collections.reduce((sum, collection) => sum + collection.totalTomes, 0)
    };
  });

  ngOnInit(): void {
    this.loadCollections();
  }

  progress(collection: UserCollectionSummary): number {
    return collection.totalTomes === 0
      ? 0
      : Math.round((collection.ownedTomes / collection.totalTomes) * 100);
  }

  // New tomes from Comic Vine are added by the server's daily catalog sync, so the
  // shelf only reads what is stored.
  private loadCollections(): void {
    this.collectionsApi.getCollections().subscribe({
      next: response => {
        this.collections.set(response.items);
        this.isLoading.set(false);
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido cargar tu estantería.')
        );
        this.isLoading.set(false);
      }
    });
  }
}
