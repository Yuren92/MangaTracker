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

  ngOnInit(): void {
    this.loadPendingTomes();
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
}