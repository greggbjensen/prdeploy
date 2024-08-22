import { Component } from '@angular/core';
import { navigation, NavItem } from '../../../app-navigation';
import { MatTreeModule, MatTreeNestedDataSource } from '@angular/material/tree';
import { KeyValuePipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-side-navigation-menu',
  templateUrl: './side-navigation-menu.component.html',
  styleUrls: ['./side-navigation-menu.component.scss'],
  standalone: true,
  imports: [MatTreeModule, KeyValuePipe, RouterModule, MatIconModule]
})
export class SideNavigationMenuComponent {
  navItems = new MatTreeNestedDataSource<NavItem>();

  constructor() {
    this.navItems.data = navigation();
  }

  childrenAccessor(navItem: NavItem) {
    return navItem.children;
  }
}
