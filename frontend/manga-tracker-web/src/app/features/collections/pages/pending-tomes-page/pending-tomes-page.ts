import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PendingTome } from '../../models/collections.models';
import { CollectionsApi } from '../../services/collections-api';
import { getApiErrorMessage } from '../../../../core/http/api-error';

@Component({
  selector: 'app-pending-tomes-page',
  imports: [RouterLink],
  templateUrl: './pending-tomes-page.html',
  styleUrl: './pending-tomes-page.scss'
})
export class PendingTomesPage implements OnInit {
  private readonly collectionsApi = inject(CollectionsApi);

  readonly pendingTomes = signal<PendingTome[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly updatingTomeIds = signal<Set<string>>(new Set<string>());

  ngOnInit(): void {
    this.loadPendingTomes();
  }

  markAsOwned(collectionId: string, tomeId: string): void {
    this.setTomeUpdating(tomeId, true);
    this.errorMessage.set(null);

    this.collectionsApi.markTomeAsOwned(collectionId, tomeId).subscribe({
      next: () => {
        this.pendingTomes.update(tomes =>
          tomes.filter(tome => tome.tomeId !== tomeId)
        );

        this.setTomeUpdating(tomeId, false);
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido marcar el tomo como comprado.')
        );

        this.setTomeUpdating(tomeId, false);
      }
    });
  }

  private loadPendingTomes(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.collectionsApi.getPendingTomes().subscribe({
      next: response => {
        this.pendingTomes.set(response.items);
        this.isLoading.set(false);
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se han podido cargar los tomos pendientes.')
        );

        this.pendingTomes.set([]);
        this.isLoading.set(false);
      }
    });
  }

  private setTomeUpdating(tomeId: string, isUpdating: boolean): void {
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