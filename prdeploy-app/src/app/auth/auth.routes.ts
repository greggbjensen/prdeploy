import { Routes } from '@angular/router';

export const authRoutes: Routes = [
  { path: 'login', loadComponent: () => import('./login/login.component').then(m => m.LoginComponent) },
  {
    path: 'login/callback',
    loadComponent: () => import('./login-callback/login-callback.component').then(m => m.LoginCallbackComponent)
  },
  {
    path: 'login/error',
    loadComponent: () => import('./login-error/login-error.component').then(m => m.LoginErrorComponent)
  },
  {
    path: 'forbidden',
    loadComponent: () => import('./forbidden/forbidden.component').then(m => m.ForbiddenComponent)
  },
  { path: 'logout', loadComponent: () => import('./logout/logout.component').then(m => m.LogoutComponent) }
];
