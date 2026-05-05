import { Component, input, output } from '@angular/core';

import { OwnedVolume } from '../../models/collection.models';

@Component({
  selector: 'app-owned-volume-row',
  imports: [],
  templateUrl: './owned-volume-row.html',
  styleUrl: './owned-volume-row.scss'
})
export class OwnedVolumeRow {
  readonly volume = input.required<OwnedVolume>();
  readonly removingVolumeNumber = input<number | null>(null);

  readonly remove = output<number>();

  onRemove(): void {
    this.remove.emit(this.volume().volumeNumber);
  }
}