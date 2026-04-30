import { Routes } from '@angular/router';
import { MainLayout } from './core/layout/main-layout/main-layout';
import { HomePage } from './features/mal/pages/home-page/home-page';
import { MangaSearchPage } from './features/mal/pages/manga-search-page/manga-search-page';
import { CollectionPage } from './features/collection/pages/collection-page/collection-page';
import { LoginPage } from './features/auth/pages/login-page/login-page';
import { RegisterPage } from './features/auth/pages/register-page/register-page';
import { ForgotPasswordPage } from './features/auth/pages/forgot-password-page/forgot-password-page';
import { authGuard } from './core/auth/auth-guard';
import { ConfirmEmailPage } from './features/auth/pages/confirm-email-page/confirm-email-page';
import { ResetPasswordPage } from './features/auth/pages/reset-password-page/reset-password-page';
import { ChangePasswordPage } from './features/auth/pages/change-password-page/change-password-page';
import { MangaDetailPage } from './features/mal/pages/manga-detail-page/manga-detail-page';


export const routes: Routes = [
     {
    path: '',
    component: MainLayout,
    children: [
      {
        path: '',
        component: HomePage
      },
      {
        path: 'search',
        component: MangaSearchPage
      },
      {
        path: 'collection',
        component: CollectionPage,
        canActivate: [authGuard]

      },
      {
        path: 'login',
        component: LoginPage
      },
      {
        path: 'register',
        component: RegisterPage
      },
      {
        path: 'forgot-password',
        component: ForgotPasswordPage
      },
      {
        path: 'confirm-email',
        component: ConfirmEmailPage
      },
      {
        path: 'reset-password',
        component: ResetPasswordPage
      },
      {
        path: 'change-password',
        component: ChangePasswordPage,
        canActivate: [authGuard]
      },
      {
        path: 'manga/:malId',
        component: MangaDetailPage,
        canActivate: [authGuard]
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
