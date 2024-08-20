import { Component, OnInit, Input, ViewChild, Inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DxTreeViewTypes } from 'devextreme-angular/ui/tree-view';
import { DxDrawerModule } from 'devextreme-angular/ui/drawer';
import { DxScrollViewModule, DxScrollViewComponent } from 'devextreme-angular/ui/scroll-view';
import { DxToolbarModule, DxToolbarTypes } from 'devextreme-angular/ui/toolbar';
import { DOCUMENT } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { ScreenService } from '../../services';
import { SideNavigationMenuComponent } from '../side-navigation-menu/side-navigation-menu.component';
import { HeaderComponent } from '../header/header.component';
import { DxiItemModule } from 'devextreme-angular/ui/nested';
import { OpenedStateMode, RevealMode } from 'devextreme/ui/drawer';

@Component({
  selector: 'app-side-nav-inner-toolbar',
  templateUrl: './side-nav-inner-toolbar.component.html',
  styleUrls: ['./side-nav-inner-toolbar.component.scss'],
  standalone: true,
  imports: [
    DxDrawerModule,
    SideNavigationMenuComponent,
    DxToolbarModule,
    DxiItemModule,
    HeaderComponent,
    DxScrollViewModule
  ]
})
export class SideNavInnerToolbarComponent implements OnInit {
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

  constructor(
    private screen: ScreenService,
    private router: Router,
    @Inject(DOCUMENT) private _document: Document
  ) {}

  ngOnInit() {
    this.menuOpened = this.screen.sizes['screen-large'];

    // Prevent memory leaks with takeUntilDestroyed, so you don't need to unsubscribe.
    this.router.events.pipe(takeUntilDestroyed()).subscribe(val => {
      if (val instanceof NavigationEnd) {
        this.selectedRoute = val.urlAfterRedirects.split('?')[0];
      }
    });

    this.screen.changed.pipe(takeUntilDestroyed()).subscribe(() => this.updateDrawer());

    this.updateDrawer();
  }

  updateDrawer() {
    const isXSmall = this.screen.sizes['screen-x-small'];
    const isLarge = this.screen.sizes['screen-large'];

    this.menuMode = isLarge ? 'shrink' : 'overlap';
    this.menuRevealMode = isXSmall ? 'slide' : 'expand';
    this.minMenuSize = isXSmall ? 0 : 60;
    this.shaderEnabled = !isLarge;
  }

  toggleMenu = (e: DxToolbarTypes.ItemClickEvent) => {
    this.menuOpened = !this.menuOpened;
    e.event?.stopPropagation();
  };

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
          this.router.navigate([path]);
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
