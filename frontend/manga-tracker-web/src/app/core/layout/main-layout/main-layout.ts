import { Component, HostListener, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthState } from '../../auth/auth-state';

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss'
})
export class MainLayout {
  readonly authState = inject(AuthState);
  readonly isUserMenuOpen = signal(false);

  private readonly router = inject(Router);

  constructor() {
    this.authState.loadCurrentUser();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.isUserMenuOpen()) {
      return;
    }

    const target = event.target as HTMLElement | null;

    if (!target?.closest('.user-menu')) {
      this.closeUserMenu();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeUserMenu();
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
