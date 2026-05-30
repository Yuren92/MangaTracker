import { Component, ElementRef, HostListener, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';

import { AuthState } from '../../auth/auth-state';

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss'
})
export class MainLayout {
  readonly authState = inject(AuthState);
  readonly isUserMenuOpen = signal(false);

  private readonly router = inject(Router);
  private readonly elementRef = inject(ElementRef<HTMLElement>);

  constructor() {
    this.authState.loadCurrentUser();
  }
  @HostListener('document:click', ['$event'])
    onDocumentClick(event: MouseEvent): void {
      if (!this.isUserMenuOpen()) {
        return;
      }

      const target = event.target as HTMLElement | null;

      if (!target) {
        return;
      }

      const clickedInsideUserMenu = target.closest('.user-menu');

      if (!clickedInsideUserMenu) {
        this.closeUserMenu();
      }
    }

  toggleUserMenu(): void {
    this.isUserMenuOpen.update(isOpen => !isOpen);
  }

  closeUserMenu(): void {
    this.isUserMenuOpen.set(false);
  }

  logout(): void {
    this.authState.clearSession();
    this.closeUserMenu();
    this.router.navigateByUrl('/login');
  }
}