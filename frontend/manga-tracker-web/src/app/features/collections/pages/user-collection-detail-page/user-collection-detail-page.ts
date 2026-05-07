import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { CollectionsApi } from '../../services/collections-api';
import { UserCollectionDetail } from '../../models/collections.models';
import { getApiErrorMessage } from '../../../../core/http/api-error';

type TomeFilter = 'all' | 'owned' | 'pending';
type TomeOrder = 'normal' | 'reverse';

@Component({
  selector: 'app-user-collection-detail-page',
  imports: [RouterLink],
  templateUrl: './user-collection-detail-page.html',
  styleUrl: './user-collection-detail-page.scss',
})
export class UserCollectionDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly collectionsApi = inject(CollectionsApi);

  readonly collection = signal<UserCollectionDetail | null>(null);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly updatingTomeIds = signal<Set<string>>(new Set<string>());
  readonly isMarkingAllAsOwned = signal(false);

  readonly tomeFilter = signal<TomeFilter>('all');
  readonly tomeOrder = signal<TomeOrder>('normal');

  readonly filteredOrderedTomes = computed(() => {
    const collection = this.collection();

    if (!collection) {
      return [];
    }

    let tomes = collection.tomes;

    if (this.tomeFilter() === 'owned') {
      tomes = tomes.filter((tome) => tome.isOwned);
    }

    if (this.tomeFilter() === 'pending') {
      tomes = tomes.filter((tome) => !tome.isOwned);
    }

    const orderedTomes = [...tomes].sort((left, right) => {
      const leftNumber = left.normalizedNumber ?? Number.MAX_SAFE_INTEGER;
      const rightNumber = right.normalizedNumber ?? Number.MAX_SAFE_INTEGER;

      return leftNumber - rightNumber;
    });

    if (this.tomeOrder() === 'reverse') {
      orderedTomes.reverse();
    }

    return orderedTomes;
  });

  ngOnInit(): void {
    const collectionId = this.route.snapshot.paramMap.get('collectionId');

    if (!collectionId) {
      this.errorMessage.set('No se ha encontrado la colección.');
      return;
    }

    this.loadCollection(collectionId);
  }

  private loadCollection(collectionId: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.collectionsApi.getCollectionDetail(collectionId).subscribe({
      next: (collection) => {
        this.collection.set(collection);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido cargar la colección.')
        );

        this.isLoading.set(false);
      },
    });
  }

  toggleTomeOwnership(tomeId: string, isOwned: boolean): void {
    const collection = this.collection();

    if (!collection) {
      return;
    }

    this.setTomeUpdating(tomeId, true);

    const request = isOwned
      ? this.collectionsApi.unmarkTomeAsOwned(collection.id, tomeId)
      : this.collectionsApi.markTomeAsOwned(collection.id, tomeId);

    request.subscribe({
      next: (result) => {
        this.collection.update((current) => {
          if (!current) {
            return current;
          }

          return {
            ...current,
            ownedTomes: result.ownedTomes,
            pendingTomes: result.pendingTomes,
            tomes: current.tomes.map((tome) =>
              tome.tomeId === result.tomeId ? { ...tome, isOwned: result.isOwned } : tome,
            ),
          };
        });

        this.setTomeUpdating(tomeId, false);
      },
      error: (error) => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido actualizar el tomo.')
        );
        this.setTomeUpdating(tomeId, false);
      },
    });
  }

  isTomeUpdating(tomeId: string): boolean {
    return this.updatingTomeIds().has(tomeId);
  }

  private setTomeUpdating(tomeId: string, isUpdating: boolean): void {
    this.updatingTomeIds.update((current) => {
      const next = new Set(current);

      if (isUpdating) {
        next.add(tomeId);
      } else {
        next.delete(tomeId);
      }

      return next;
    });
  }

  markAllAsOwned(): void {
    const collection = this.collection();

    if (!collection || collection.pendingTomes === 0) {
      return;
    }

    this.isMarkingAllAsOwned.set(true);
    this.errorMessage.set(null);

    this.collectionsApi.markAllTomesAsOwned(collection.id).subscribe({
      next: result => {
        this.collection.update(current => {
          if (!current) {
            return current;
          }

          return {
            ...current,
            ownedTomes: result.ownedTomes,
            pendingTomes: result.pendingTomes,
            tomes: current.tomes.map(tome => ({
              ...tome,
              isOwned: true
            }))
          };
        });

        this.isMarkingAllAsOwned.set(false);
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se han podido marcar todos los tomos.')
        );

        this.isMarkingAllAsOwned.set(false);
      }
    });
  }
}
