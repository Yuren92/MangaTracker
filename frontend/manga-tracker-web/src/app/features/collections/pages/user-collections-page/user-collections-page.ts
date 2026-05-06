import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { UserCollectionSummary } from '../../models/collections.models';
import { CollectionsApi } from '../../services/collections-api';

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
          error?.error?.detail ?? 'No se han podido cargar tus colecciones.'
        );

        this.collections.set([]);
        this.isLoading.set(false);
      }
    });
  }
}