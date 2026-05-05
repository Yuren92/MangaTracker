import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CollectionApi } from '../../../collection/services/collection-api';
import { MalMangaDetail } from '../../models/mal.models';
import { MalApi } from '../../services/mal-api';
import { getApiErrorMessage } from '../../../../core/http/api-error';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';
import { MangaCard } from '../../../../shared/components/manga-card/manga-card';

@Component({
  selector: 'app-manga-detail-page',
  imports: [RouterLink, AppAlert, MangaCard],
  templateUrl: './manga-detail-page.html',
  styleUrl: './manga-detail-page.scss'
})
export class MangaDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly malApi = inject(MalApi);

  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly manga = signal<MalMangaDetail | null>(null);

  private readonly collectionApi = inject(CollectionApi);
  private readonly router = inject(Router);

  readonly isAddingToCollection = signal(false);
  readonly addToCollectionError = signal<string | null>(null);

  readonly hasRecommendations = computed(
    () => (this.manga()?.recommendations.length ?? 0) > 0
  );

  constructor() {
    this.route.paramMap.subscribe(params => {
      const malIdParam = params.get('malId');
      const malId = Number(malIdParam);

      if (!malId || Number.isNaN(malId)) {
        this.isLoading.set(false);
        this.errorMessage.set('El identificador del manga no es válido.');
        return;
      }
        
      this.loadMangaDetail(malId);
    });
  }

  private loadMangaDetail(malId: number): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.addToCollectionError.set(null);
    this.manga.set(null);

    this.malApi.getMangaDetail(malId).subscribe({
      next: response => {
        this.manga.set(response.item);
        this.isLoading.set(false);
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido cargar el detalle del manga.')
        );

        this.isLoading.set(false);
      }
    });
  }

  addToCollection(): void {
    const currentManga = this.manga();

    if (!currentManga) {
      return;
    }

    this.isAddingToCollection.set(true);
    this.addToCollectionError.set(null);

    this.collectionApi.addMangaToCollection({
      malId: currentManga.malId,
      customTotalVolumes: currentManga.totalVolumes
    }).subscribe({
      next: response => {
        this.router.navigate(['/collection', response.id]);
      },
      error: error => {
        this.addToCollectionError.set(
          getApiErrorMessage(error, 'No se ha podido añadir el manga a tu colección.')
        );

        this.isAddingToCollection.set(false);
      }
    });
  }
}