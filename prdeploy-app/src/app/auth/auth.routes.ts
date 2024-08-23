import { Routes } from '@angular/router';
import { AuthGuard } from '../shared/services';

export const authRoutes: Routes = [
  {
    path: 'auth/secure-redirect',
    canActivate: [AuthGuard],
    loadComponent: () => import('./secure-redirect/secure-redirect.component').then(m => m.SecureRedirectComponent)
  },
  {
    path: 'login/callback',
    loadComponent: () => import('./login-callback/login-callback.component').then(m => m.LoginCallbackComponent)
  },
  {
    path: 'login/error',
    loadComponent: () => import('./login-error/login-error.component').then(m => m.LoginErrorComponent)
  },
  {
    path: 'login',
    loadComponent: () => import('./login/login.component').then(m => m.LoginComponent)
  },
  { path: 'logout', loadComponent: () => import('./logout/logout.component').then(m => m.LogoutComponent) }
];
