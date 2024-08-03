import { Routes } from '@angular/router';

export const authRoutes: Routes = [
  {
    path: 'login/callback',
    loadComponent: () => import('./login-callback/login-callback.component').then(m => m.LoginCallbackComponent)
  },
  {
    path: 'login/error',
    loadComponent: () => import('./login-error/login-error.component').then(m => m.LoginErrorComponent)
  },
  { path: 'logout', loadComponent: () => import('./logout/logout.component').then(m => m.LogoutComponent) }
];
