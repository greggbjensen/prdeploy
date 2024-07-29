import { Routes } from '@angular/router';
import { QueueComponent } from './queue';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'queue',
    pathMatch: 'full'
  },
  {
    path: 'queue',
    component: QueueComponent
  }
];
