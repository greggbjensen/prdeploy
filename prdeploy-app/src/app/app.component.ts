import { Component, HostBinding } from '@angular/core';
import { ScreenService, AppInfoService } from './shared/services';
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
  @HostBinding('class') get getClass() {
    return Object.keys(this.screen.sizes)
      .filter(cl => this.screen.sizes[cl])
      .join(' ');
  }

  sideNavMode: SideNavMode = 'full';

  constructor(
    private screen: ScreenService,
    public appInfo: AppInfoService
  ) {}

  toggleMenu() {
    this.sideNavMode = this.sideNavMode == 'full' ? 'icons' : 'full';
  }
}
