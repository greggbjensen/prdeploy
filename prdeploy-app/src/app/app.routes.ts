import { Routes } from '@angular/router';
import { AuthGuard } from 'src/app/shared/guards';
import { authRoutes } from './auth/auth.routes';

export const routes: Routes = [
  {
    path: '',
    canActivate: [AuthGuard], // Guard all routes with empty parent.
    children: [
      {
        path: '',
        redirectTo: 'deployments/queue',
        pathMatch: 'full'
      },
      {
        path: 'deployments',
        loadChildren: () => import('./deployments/deployments.routes').then(m => m.routes)
      }
    ]
  },
  ...authRoutes,
  {
    path: '**',
    redirectTo: 'deployments/queue'
  }
];
