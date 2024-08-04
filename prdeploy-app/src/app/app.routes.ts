import { Routes } from '@angular/router';
import { authRoutes } from './auth/auth.routes';
import { AuthGuard } from './shared/services';
import { HomeComponent } from './home/home.component';

export const routes: Routes = [
  {
    path: '',
    component: HomeComponent
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
  },
  ...authRoutes,
  {
    path: '**',
    redirectTo: 'deployments'
  }
];
