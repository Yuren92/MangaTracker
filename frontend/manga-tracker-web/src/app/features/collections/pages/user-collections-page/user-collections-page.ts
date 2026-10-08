import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { getApiErrorMessage } from '../../../../core/http/api-error';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';
import { CoverPipe } from '../../../../shared/pipes/cover.pipe';
import { TomesPipe } from '../../../../shared/pipes/tomes.pipe';
import { UserCollectionSummary } from '../../models/collections.models';
import { CollectionsApi, IMPORT_POLL_MS } from '../../services/collections-api';

@Component({
  selector: 'app-user-collections-page',
  imports: [RouterLink, AppAlert, CoverPipe, TomesPipe],
  templateUrl: './user-collections-page.html',
  styleUrl: './user-collections-page.scss'
})
export class UserCollectionsPage implements OnInit {
  private readonly collectionsApi = inject(CollectionsApi);
  private pollTimer: ReturnType<typeof setTimeout> | undefined;

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

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.pollTimer));
  }

  ngOnInit(): void {
    this.loadCollections();
  }

  progress(collection: UserCollectionSummary): number {
    return collection.totalTomes === 0
      ? 0
      : Math.round((collection.ownedTomes / collection.totalTomes) * 100);
  }

  // New tomes from Comic Vine are added by the server's daily catalog sync, so the
  // shelf only reads what is stored. A series added moments ago may still be
  // downloading its tomes; while one is, the shelf reloads every few seconds.
  private loadCollections(): void {
    this.collectionsApi.getCollections().subscribe({
      next: response => {
        this.collections.set(response.items);
        this.isLoading.set(false);
        this.errorMessage.set(null);

        if (response.items.some(collection => collection.isImporting)) {
          this.pollTimer = setTimeout(() => this.loadCollections(), IMPORT_POLL_MS);
        }
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
