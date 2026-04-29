import { Routes } from '@angular/router';
import { MainLayout } from './core/layout/main-layout/main-layout';
import { HomePage } from './features/mal/pages/home-page/home-page';
import { MangaSearchPage } from './features/mal/pages/manga-search-page/manga-search-page';
import { CollectionPage } from './features/collection/pages/collection-page/collection-page';
import { LoginPage } from './features/auth/pages/login-page/login-page';
import { RegisterPage } from './features/auth/pages/register-page/register-page';


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
        component: CollectionPage
      },
      {
        path: 'login',
        component: LoginPage
      },
      {
        path: 'register',
        component: RegisterPage
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
