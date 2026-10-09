import { DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { getApiErrorMessage } from '../../../../core/http/api-error';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';
import { CoverPipe } from '../../../../shared/pipes/cover.pipe';
import { TomesPipe } from '../../../../shared/pipes/tomes.pipe';
import { UserCollectionDetail, UserCollectionTome } from '../../models/collections.models';
import { CollectionsApi, IMPORT_POLL_MS } from '../../services/collections-api';

type TomeFilter = 'all' | 'missing' | 'owned';

@Component({
  selector: 'app-user-collection-detail-page',
  imports: [RouterLink, DatePipe, AppAlert, CoverPipe, TomesPipe],
  templateUrl: './user-collection-detail-page.html',
  styleUrl: './user-collection-detail-page.scss'
})
export class UserCollectionDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly collectionsApi = inject(CollectionsApi);
  private pollTimer: ReturnType<typeof setTimeout> | undefined;

  readonly collection = signal<UserCollectionDetail | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly updatingTomeIds = signal<ReadonlySet<string>>(new Set<string>());
  readonly isMarkingAll = signal(false);
  readonly isDeleting = signal(false);
  readonly filter = signal<TomeFilter>('all');

  readonly progress = computed(() => {
    const collection = this.collection();

    return collection && collection.totalTomes > 0
      ? Math.round((collection.ownedTomes / collection.totalTomes) * 100)
      : 0;
  });

  // The API already returns tomes in reading order (number, then specials).
  readonly visibleTomes = computed(() => {
    const tomes = this.collection()?.tomes ?? [];

    switch (this.filter()) {
      case 'missing':
        return tomes.filter(tome => !tome.isOwned);
      case 'owned':
        return tomes.filter(tome => tome.isOwned);
      default:
        return tomes;
    }
  });

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.pollTimer));
  }

  ngOnInit(): void {
    const collectionId = this.route.snapshot.paramMap.get('collectionId');

    if (!collectionId) {
      this.isLoading.set(false);
      this.errorMessage.set('No se ha encontrado la serie.');
      return;
    }

    this.loadCollection(collectionId);
  }

  tomeLabel(tome: UserCollectionTome): string {
    const number = tome.issueNumber ? `Tomo ${tome.issueNumber}` : 'Tomo sin número';
    const title = tome.title ? `, ${tome.title}` : '';

    return `${number}${title}`;
  }

  isUpdating(tomeId: string): boolean {
    return this.updatingTomeIds().has(tomeId);
  }

  toggle(tome: UserCollectionTome): void {
    const collection = this.collection();

    if (!collection || this.isUpdating(tome.tomeId)) {
      return;
    }

    this.setUpdating(tome.tomeId, true);
    this.errorMessage.set(null);

    const request = tome.isOwned
      ? this.collectionsApi.unmarkTomeAsOwned(collection.id, tome.tomeId)
      : this.collectionsApi.markTomeAsOwned(collection.id, tome.tomeId);

    request.subscribe({
      next: result => {
        this.collection.update(current => current && {
          ...current,
          ownedTomes: result.ownedTomes,
          pendingTomes: result.pendingTomes,
          tomes: current.tomes.map(item =>
            item.tomeId === result.tomeId ? { ...item, isOwned: result.isOwned } : item)
        });
        this.setUpdating(tome.tomeId, false);
      },
      error: error => {
        this.errorMessage.set(getApiErrorMessage(error, 'No se ha podido actualizar el tomo.'));
        this.setUpdating(tome.tomeId, false);
      }
    });
  }

  markAllAsOwned(): void {
    const collection = this.collection();

    if (!collection || collection.pendingTomes === 0) {
      return;
    }

    this.isMarkingAll.set(true);
    this.errorMessage.set(null);

    this.collectionsApi.markAllTomesAsOwned(collection.id).subscribe({
      next: result => {
        this.collection.update(current => current && {
          ...current,
          ownedTomes: result.ownedTomes,
          pendingTomes: result.pendingTomes,
          tomes: current.tomes.map(tome => ({ ...tome, isOwned: true }))
        });
        this.isMarkingAll.set(false);
      },
      error: error => {
        this.errorMessage.set(getApiErrorMessage(error, 'No se han podido marcar todos los tomos.'));
        this.isMarkingAll.set(false);
      }
    });
  }

  removeFromShelf(): void {
    const collection = this.collection();

    if (!collection) {
      return;
    }

    const confirmed = window.confirm(
      `¿Quitar «${collection.title}» de tu estantería? Se perderá qué tomos tienes marcados.`
    );

    if (!confirmed) {
      return;
    }

    this.isDeleting.set(true);
    this.errorMessage.set(null);

    this.collectionsApi.deleteCollection(collection.id).subscribe({
      next: () => this.router.navigateByUrl('/collections'),
      error: error => {
        this.errorMessage.set(getApiErrorMessage(error, 'No se ha podido quitar la serie.'));
        this.isDeleting.set(false);
      }
    });
  }

  // A series added moments ago may still be downloading its tomes; while it is, the
  // page re-reads it every few seconds so they appear as they arrive. A reload that
  // lands while a tome is being marked is skipped, so it cannot undo that click.
  private loadCollection(collectionId: string): void {
    this.collectionsApi.getCollectionDetail(collectionId).subscribe({
      next: collection => {
        const isBusy = this.updatingTomeIds().size > 0 || this.isMarkingAll();

        if (!isBusy || !this.collection()) {
          this.collection.set(collection);
        }

        this.isLoading.set(false);

        if (collection.isImporting || isBusy) {
          this.pollTimer = setTimeout(() => this.loadCollection(collectionId), IMPORT_POLL_MS);
        }
      },
      error: error => {
        this.errorMessage.set(getApiErrorMessage(error, 'No se ha podido cargar la serie.'));
        this.isLoading.set(false);
      }
    });
  }

  private setUpdating(tomeId: string, isUpdating: boolean): void {
    this.updatingTomeIds.update(current => {
      const next = new Set(current);

      if (isUpdating) {
        next.add(tomeId);
      } else {
        next.delete(tomeId);
      }

      return next;
    });
  }
}
