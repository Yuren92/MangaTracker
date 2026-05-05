import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-manga-card',
  imports: [RouterLink],
  templateUrl: './manga-card.html',
  styleUrl: './manga-card.scss'
})
export class MangaCard {
  readonly title = input.required<string>();
  readonly imageUrl = input<string | null>(null);
  readonly subtitle = input<string | null>(null);
  readonly detailLink = input.required<unknown[]>();
  readonly actionText = input<string>('Ver detalle');
}