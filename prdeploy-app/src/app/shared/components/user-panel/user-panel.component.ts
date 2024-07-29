import { Component, Input } from '@angular/core';
import { DxListModule } from 'devextreme-angular/ui/list';
import { DxContextMenuModule } from 'devextreme-angular/ui/context-menu';
import { User } from 'src/app/shared/models';

@Component({
  selector: 'app-user-panel',
  templateUrl: 'user-panel.component.html',
  styleUrls: ['./user-panel.component.scss'],
  standalone: true,
  imports: [DxContextMenuModule, DxListModule]
})
export class UserPanelComponent {
  @Input()
  menuItems: any;

  @Input()
  menuMode!: string;

  @Input()
  set user(value: User) {
    this._user = value;
    if (this._user) {
      this.userInitials = this._user.firstName ? this._user.firstName.charAt(0).toUpperCase() : '';
      if (this._user.lastName) {
        this.userInitials += this._user.lastName.charAt(0).toUpperCase();
      }
    }
  }

  get user(): User {
    return this._user;
  }

  userInitials: string;
  private _user: User;

  constructor() {}
}
