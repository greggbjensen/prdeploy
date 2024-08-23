import { Component, HostListener } from '@angular/core';
import { AuthService } from './shared/services';
import { RouterOutlet } from '@angular/router';
import { FooterComponent, SideNavigationMenuComponent, HeaderComponent } from './shared/components';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSidenavModule, MatDrawerMode } from '@angular/material/sidenav';
import { SideNavMode } from './shared/components/side-navigation-menu/side-nav-mode';
import { BreakpointObserver } from '@angular/cdk/layout';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NgClass } from '@angular/common';

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
    HeaderComponent,
    NgClass
  ]
})
export class AppComponent {
  @HostListener('window:resize')
  onResize() {
    this.updateBackdrop();
  }

  drawerMode: MatDrawerMode = 'side';
  sideNavMode: SideNavMode = 'full';
  sideNavHidden = true;
  hasBackdrop = false;
  isMobile = false;
  isAuthenticated = false;

  constructor(
    private _breakpointObserver: BreakpointObserver,
    private _authService: AuthService
  ) {
    this.updateBackdrop();

    this._breakpointObserver
      .observe('(max-width: 768px)')
      .pipe(takeUntilDestroyed())
      .subscribe(state => {
        this.isMobile = state.matches;
      });

    this._authService.isAuthenticated$.pipe(takeUntilDestroyed()).subscribe((isAuthenticated: boolean) => {
      this.isAuthenticated = isAuthenticated;
      this.updateBackdrop();
    });
  }

  updateBackdrop() {
    if (this.isAuthenticated) {
      this.sideNavHidden = false;
      this.hasBackdrop = this.isMobile && this.sideNavMode === 'full';
      this.drawerMode = this.hasBackdrop ? 'over' : 'side';
    } else {
      this.sideNavHidden = true;
      this.hasBackdrop = false;
      this.drawerMode = 'side';
    }
  }

  toggleMenu() {
    this.sideNavMode = this.sideNavMode == 'full' ? 'icons' : 'full';
    this.updateBackdrop();
  }
}
