import { Component } from '@angular/core';
import { AppInfoService } from './shared/services';
import { RouterOutlet } from '@angular/router';
import { FooterComponent, SideNavigationMenuComponent, HeaderComponent } from './shared/components';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { SideNavMode } from './shared/components/side-navigation-menu/side-nav-mode';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  standalone: true,
  imports: [
    RouterOutlet,
    FooterComponent,
    MatSnackBarModule,
    MatSidenavModule,
    SideNavigationMenuComponent,
    HeaderComponent
  ]
})
export class AppComponent {
  sideNavMode: SideNavMode = 'full';

  constructor(public appInfo: AppInfoService) {}

  toggleMenu() {
    this.sideNavMode = this.sideNavMode == 'full' ? 'icons' : 'full';
  }
}
