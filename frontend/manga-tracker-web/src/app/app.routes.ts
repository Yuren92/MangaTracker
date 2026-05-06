import { Routes } from '@angular/router';
import { MainLayout } from './core/layout/main-layout/main-layout';
import { LoginPage } from './features/auth/pages/login-page/login-page';
import { RegisterPage } from './features/auth/pages/register-page/register-page';
import { ForgotPasswordPage } from './features/auth/pages/forgot-password-page/forgot-password-page';
import { authGuard } from './core/auth/auth-guard';
import { ConfirmEmailPage } from './features/auth/pages/confirm-email-page/confirm-email-page';
import { ResetPasswordPage } from './features/auth/pages/reset-password-page/reset-password-page';
import { ChangePasswordPage } from './features/auth/pages/change-password-page/change-password-page';
import { guestGuard } from './core/auth/guest-guard';
import { CatalogSearchPage } from './features/catalog/pages/catalog-search-page/catalog-search-page';
import { UserCollectionDetailPage } from './features/collections/pages/user-collection-detail-page/user-collection-detail-page';
import { UserCollectionsPage } from './features/collections/pages/user-collections-page/user-collections-page';
import { PendingTomesPage } from './features/collections/pages/pending-tomes-page/pending-tomes-page';


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
        component: LoginPage,
        canActivate: [guestGuard]
      },
      {
        path: 'register',
        component: RegisterPage,
        canActivate: [guestGuard]
      },
      {
        path: 'forgot-password',
        component: ForgotPasswordPage,
        canActivate: [guestGuard]
      },
      {
        path: 'confirm-email',
        component: ConfirmEmailPage
      },
      {
        path: 'reset-password',
        component: ResetPasswordPage,
        canActivate: [guestGuard]
      },
      {
        path: 'change-password',
        component: ChangePasswordPage,
        canActivate: [authGuard]
      },
      {
        path: 'collections/pending-tomes',
        component: PendingTomesPage,
        canActivate: [authGuard]
      },
      {
        path: 'collections/:collectionId',
        component: UserCollectionDetailPage,
        canActivate: [authGuard]
      },
      {
        path: 'collections',
        component: UserCollectionsPage,
        canActivate: [authGuard]
      },
      {
        path: 'catalog/search',
        component: CatalogSearchPage,
        canActivate: [authGuard]
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
