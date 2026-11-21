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
  },
  {
    // Deep link to a section of the secondary left nav, e.g. owner/slack.
    path: ':level/:section',
    component: SettingsComponent
  }
];
