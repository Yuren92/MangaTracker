import { DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { getApiErrorMessage } from '../../../../core/http/api-error';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';
import { PendingTome } from '../../models/collections.models';
import { CollectionsApi } from '../../services/collections-api';
import { localToday, splitByRelease } from './release-split';

interface PendingSeries {
  collectionId: string;
  title: string;
  publisherName: string | null;
  tomes: PendingTome[];
}

@Component({
  selector: 'app-pending-tomes-page',
  imports: [RouterLink, DatePipe, AppAlert],
  templateUrl: './pending-tomes-page.html',
  styleUrl: './pending-tomes-page.scss'
})
export class PendingTomesPage implements OnInit {
  private readonly collectionsApi = inject(CollectionsApi);

  readonly pendingTomes = signal<PendingTome[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly updatingTomeIds = signal<ReadonlySet<string>>(new Set<string>());

  // The last tome marked from this page, so a mistaken click can be undone.
  readonly lastMarked = signal<PendingTome | null>(null);

  // Comic Vine lists tomes before they go on sale: those are "coming soon", not missing.
  private readonly split = computed(() => splitByRelease(this.pendingTomes(), localToday()));
  readonly released = computed(() => this.split().released);
  readonly upcoming = computed(() => this.split().upcoming);

  // The API returns tomes ordered by series and number; grouping keeps that order,
  // so the first tome of each series is the next one to buy.
  readonly series = computed<PendingSeries[]>(() => {
    const groups = new Map<string, PendingSeries>();

    for (const tome of this.released()) {
      const group = groups.get(tome.collectionId) ?? {
        collectionId: tome.collectionId,
        title: tome.seriesTitle,
        publisherName: tome.publisherName,
        tomes: []
      };

      group.tomes.push(tome);
      groups.set(tome.collectionId, group);
    }

    return [...groups.values()];
  });

  ngOnInit(): void {
    this.collectionsApi.getPendingTomes().subscribe({
      next: response => {
        this.pendingTomes.set(response.items);
        this.isLoading.set(false);
      },
      error: error => {
        this.errorMessage.set(getApiErrorMessage(error, 'No se ha podido cargar lo que te falta.'));
        this.isLoading.set(false);
      }
    });
  }

  tomeName(tome: PendingTome): string {
    return tome.issueNumber ? `Tomo ${tome.issueNumber}` : 'Tomo sin número';
  }

  isUpdating(tomeId: string): boolean {
    return this.updatingTomeIds().has(tomeId);
  }

  markAsOwned(tome: PendingTome): void {
    this.setUpdating(tome.tomeId, true);
    this.errorMessage.set(null);

    this.collectionsApi.markTomeAsOwned(tome.collectionId, tome.tomeId).subscribe({
      next: () => {
        this.pendingTomes.update(tomes => tomes.filter(item => item.tomeId !== tome.tomeId));
        this.lastMarked.set(tome);
        this.setUpdating(tome.tomeId, false);
      },
      error: error => {
        this.errorMessage.set(getApiErrorMessage(error, 'No se ha podido marcar el tomo.'));
        this.setUpdating(tome.tomeId, false);
      }
    });
  }

  undo(): void {
    const tome = this.lastMarked();

    if (!tome) {
      return;
    }

    this.lastMarked.set(null);

    this.collectionsApi.unmarkTomeAsOwned(tome.collectionId, tome.tomeId).subscribe({
      // Back where it was: the list is re-sorted the way the API sorts it.
      next: () => this.pendingTomes.update(tomes => [...tomes, tome].sort(byShelfOrder)),
      error: error => this.errorMessage.set(getApiErrorMessage(error, 'No se ha podido deshacer.'))
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

// Same order as the API: series title, publisher, then tome number (unnumbered last).
function byShelfOrder(left: PendingTome, right: PendingTome): number {
  return left.seriesTitle.localeCompare(right.seriesTitle)
    || (left.publisherName ?? '').localeCompare(right.publisherName ?? '')
    || (left.normalizedNumber ?? Number.MAX_SAFE_INTEGER) - (right.normalizedNumber ?? Number.MAX_SAFE_INTEGER)
    || left.issueNumber.localeCompare(right.issueNumber);
}
