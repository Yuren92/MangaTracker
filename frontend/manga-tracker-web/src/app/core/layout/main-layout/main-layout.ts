import { Component, inject, signal } from '@angular/core';
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

  constructor() {
    this.authState.loadCurrentUser();
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