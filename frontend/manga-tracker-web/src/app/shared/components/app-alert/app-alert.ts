import { Component, input } from '@angular/core';

export type AppAlertType = 'success' | 'error' | 'info';

@Component({
  selector: 'app-alert',
  imports: [],
  templateUrl: './app-alert.html',
  styleUrl: './app-alert.scss'
})
export class AppAlert {
  readonly type = input.required<AppAlertType>();
  readonly message = input<string | null>(null);
}