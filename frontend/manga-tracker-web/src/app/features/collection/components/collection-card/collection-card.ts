import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';

import { MangaCollectionListItem } from '../../models/collection.models';

@Component({
  selector: 'app-collection-card',
  imports: [RouterLink],
  templateUrl: './collection-card.html',
  styleUrl: './collection-card.scss'
})
export class CollectionCard {
  readonly item = input.required<MangaCollectionListItem>();
  readonly removingItemId = input<string | null>(null);

  readonly remove = output<MangaCollectionListItem>();

  onRemove(): void {
    this.remove.emit(this.item());
  }
}