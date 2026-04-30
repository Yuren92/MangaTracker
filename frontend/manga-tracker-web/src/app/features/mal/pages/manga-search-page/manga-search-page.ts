import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { MalMangaSearchResult } from '../../models/mal.models';
import { MalApi } from '../../services/mal-api';

@Component({
  selector: 'app-manga-search-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './manga-search-page.html',
  styleUrl: './manga-search-page.scss'
})
export class MangaSearchPage {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly malApi = inject(MalApi);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly results = signal<MalMangaSearchResult[]>([]);

  readonly form = this.formBuilder.group({
    query: ['', [Validators.required, Validators.minLength(3)]]
  });

  search(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const query = this.form.controls.query.value.trim();

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.results.set([]);

    this.malApi.searchManga(query, 10).subscribe({
      next: response => {
        this.results.set(response.items);
        this.isLoading.set(false);
      },
      error: error => {
        this.errorMessage.set(
          error?.error?.detail ?? 'No se ha podido buscar manga.'
        );

        this.isLoading.set(false);
      }
    });
  }
}