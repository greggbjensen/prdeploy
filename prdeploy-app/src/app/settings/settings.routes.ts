import { Routes } from '@angular/router';
import { SettingsComponent } from './settings.component';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'owner',
    pathMatch: 'full'
  },
  {
    path: ':level',
    component: SettingsComponent
  }
];
