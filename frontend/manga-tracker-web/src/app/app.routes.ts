import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth-guard';
import { guestGuard } from './core/auth/guest-guard';
import { MainLayout } from './core/layout/main-layout/main-layout';

// Every page is loaded on demand: the initial bundle only carries the layout, and each
// screen is downloaded the first time it is visited.
export const routes: Routes = [
  {
    path: '',
    component: MainLayout,
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'collections'
      },
      {
        path: 'login',
        title: 'Iniciar sesión · Manga Tracker',
        canActivate: [guestGuard],
        loadComponent: () =>
          import('./features/auth/pages/login-page/login-page').then(m => m.LoginPage)
      },
      {
        path: 'register',
        title: 'Crear cuenta · Manga Tracker',
        canActivate: [guestGuard],
        loadComponent: () =>
          import('./features/auth/pages/register-page/register-page').then(m => m.RegisterPage)
      },
      {
        path: 'forgot-password',
        title: 'Recuperar contraseña · Manga Tracker',
        canActivate: [guestGuard],
        loadComponent: () =>
          import('./features/auth/pages/forgot-password-page/forgot-password-page').then(m => m.ForgotPasswordPage)
      },
      {
        path: 'resend-confirmation',
        title: 'Reenviar confirmación · Manga Tracker',
        canActivate: [guestGuard],
        loadComponent: () =>
          import('./features/auth/pages/resend-confirmation-page/resend-confirmation-page').then(m => m.ResendConfirmationPage)
      },
      {
        path: 'confirm-email',
        title: 'Confirmar email · Manga Tracker',
        loadComponent: () =>
          import('./features/auth/pages/confirm-email-page/confirm-email-page').then(m => m.ConfirmEmailPage)
      },
      {
        path: 'reset-password',
        title: 'Restablecer contraseña · Manga Tracker',
        canActivate: [guestGuard],
        loadComponent: () =>
          import('./features/auth/pages/reset-password-page/reset-password-page').then(m => m.ResetPasswordPage)
      },
      {
        path: 'change-password',
        title: 'Cambiar contraseña · Manga Tracker',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/auth/pages/change-password-page/change-password-page').then(m => m.ChangePasswordPage)
      },
      {
        // Old address, kept so bookmarks and earlier links still work.
        path: 'collections/pending-tomes',
        redirectTo: 'pending'
      },
      {
        path: 'pending',
        title: 'Me faltan · Manga Tracker',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/collections/pages/pending-tomes-page/pending-tomes-page').then(m => m.PendingTomesPage)
      },
      {
        path: 'collections/:collectionId',
        title: 'Serie · Manga Tracker',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/collections/pages/user-collection-detail-page/user-collection-detail-page').then(m => m.UserCollectionDetailPage)
      },
      {
        path: 'collections',
        title: 'Mi estantería · Manga Tracker',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/collections/pages/user-collections-page/user-collections-page').then(m => m.UserCollectionsPage)
      },
      {
        path: 'catalog/search',
        title: 'Añadir serie · Manga Tracker',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/catalog/pages/catalog-search-page/catalog-search-page').then(m => m.CatalogSearchPage)
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
