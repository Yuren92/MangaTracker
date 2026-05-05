import { Component, input } from '@angular/core';

@Component({
  selector: 'app-missing-volumes-panel',
  imports: [],
  templateUrl: './missing-volumes-panel.html',
  styleUrl: './missing-volumes-panel.scss'
})
export class MissingVolumesPanel {
  readonly missingVolumeNumbers = input.required<number[]>();
}