import { Routes } from '@angular/router';
import { authRoutes } from './auth/auth.routes';
import { AuthGuard } from './shared/services';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  ...authRoutes,
  {
    path: 'repositories',
    canActivate: [AuthGuard],
    loadChildren: () => import('./repositories/repositories.routes').then(m => m.routes)
  },
  {
    path: ':owner/:repo',
    children: [
      {
        path: '',
        redirectTo: 'deployments',
        pathMatch: 'full'
      },
      {
        path: 'deployments',
        canActivate: [AuthGuard],
        loadChildren: () => import('./deployments/deployments.routes').then(m => m.routes)
      },
      {
        path: 'environments',
        canActivate: [AuthGuard],
        loadChildren: () => import('./environments/environments.routes').then(m => m.routes)
      },

      {
        path: 'settings',
        canActivate: [AuthGuard],
        loadChildren: () => import('./settings/settings.routes').then(m => m.routes)
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'deployments'
  }
];
