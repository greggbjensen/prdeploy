import { Component, OnInit, Input, ViewChild, Inject, DestroyRef } from '@angular/core';
import { DxTreeViewTypes } from 'devextreme-angular/ui/tree-view';
import { DxDrawerModule } from 'devextreme-angular/ui/drawer';
import { DxScrollViewModule, DxScrollViewComponent } from 'devextreme-angular/ui/scroll-view';
import { DOCUMENT } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { AuthService, ScreenService } from '../../services';
import { SideNavigationMenuComponent } from '../side-navigation-menu/side-navigation-menu.component';
import { HeaderComponent } from '../header/header.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DxTemplateModule } from 'devextreme-angular/core';
import { OpenedStateMode, RevealMode } from 'devextreme/ui/drawer';

@Component({
  selector: 'app-side-nav-outer-toolbar',
  templateUrl: './side-nav-outer-toolbar.component.html',
  styleUrls: ['./side-nav-outer-toolbar.component.scss'],
  standalone: true,
  imports: [HeaderComponent, DxDrawerModule, DxTemplateModule, SideNavigationMenuComponent, DxScrollViewModule]
})
export class SideNavOuterToolbarComponent implements OnInit {
  @ViewChild(DxScrollViewComponent, { static: true }) scrollView!: DxScrollViewComponent;
  selectedRoute = '';

  menuOpened!: boolean;
  temporaryMenuOpened = false;

  @Input()
  title!: string;

  menuMode: OpenedStateMode = 'shrink';
  menuRevealMode: RevealMode = 'expand';
  minMenuSize = 0;
  shaderEnabled = false;
  isAuthenticated = false;

  constructor(
    private _screen: ScreenService,
    private _router: Router,
    private _authService: AuthService,
    @Inject(DOCUMENT) private _document: Document,
    private _destroyRef: DestroyRef
  ) {
    this._authService.isAuthenticated$
      .pipe(takeUntilDestroyed())
      .subscribe((isAuthenticated: boolean) => (this.isAuthenticated = isAuthenticated));
  }

  ngOnInit() {
    this.menuOpened = this._screen.sizes['screen-large'];

    this._router.events.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(val => {
      if (val instanceof NavigationEnd) {
        this.selectedRoute = val.urlAfterRedirects.split('?')[0];
      }
    });

    this._screen.changed.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(() => this.updateDrawer());

    this.updateDrawer();
  }

  updateDrawer() {
    const isXSmall = this._screen.sizes['screen-x-small'];
    const isLarge = this._screen.sizes['screen-large'];

    this.menuMode = isLarge ? 'shrink' : 'overlap';
    this.menuRevealMode = isXSmall ? 'slide' : 'expand';
    this.minMenuSize = isXSmall ? 0 : 60;
    this.shaderEnabled = !isLarge;
  }

  get hideMenuAfterNavigation() {
    return this.menuMode === 'overlap' || this.temporaryMenuOpened;
  }

  get showMenuAfterClick() {
    return !this.menuOpened;
  }

  navigationChanged(event: DxTreeViewTypes.ItemClickEvent) {
    const path = (event.itemData as any).path;
    const pointerEvent = event.event;

    if (path && this.menuOpened) {
      if (event.node?.selected) {
        pointerEvent?.preventDefault();
      } else {
        if (/https?:\/\//.test(path)) {
          this._document.location.href = path;
        } else {
          this._router.navigate([path]);
          this.scrollView.instance.scrollTo(0);
        }
      }

      if (this.hideMenuAfterNavigation) {
        this.temporaryMenuOpened = false;
        this.menuOpened = false;
        pointerEvent?.stopPropagation();
      }
    } else {
      pointerEvent?.preventDefault();
    }
  }

  navigationClick() {
    if (this.showMenuAfterClick) {
      this.temporaryMenuOpened = true;
      this.menuOpened = true;
    }
  }
}
