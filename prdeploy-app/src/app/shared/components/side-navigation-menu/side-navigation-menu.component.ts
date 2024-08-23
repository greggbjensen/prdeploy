import { Component } from '@angular/core';
import { navigation, NavItem } from '../../../app-navigation';
import { MatTreeModule, MatTreeNestedDataSource } from '@angular/material/tree';
import { KeyValuePipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { RepoManager } from '../../managers';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-side-navigation-menu',
  templateUrl: './side-navigation-menu.component.html',
  styleUrls: ['./side-navigation-menu.component.scss'],
  standalone: true,
  imports: [MatTreeModule, KeyValuePipe, RouterModule, MatIconModule]
})
export class SideNavigationMenuComponent {
  navItems = new MatTreeNestedDataSource<NavItem>();

  constructor(private _repoManager: RepoManager) {
    this._repoManager.valueChanged$.pipe(takeUntilDestroyed()).subscribe(() => {
      this.navItems.data = navigation(this._repoManager.owner, this._repoManager.repo);
    });
  }

  childrenAccessor(navItem: NavItem) {
    return navItem.children;
  }
}
