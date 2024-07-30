import { Component, Input, Output, EventEmitter } from '@angular/core';
import { UserPanelComponent } from '../user-panel/user-panel.component';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DxToolbarModule } from 'devextreme-angular/ui/toolbar';

import { User } from 'src/app/shared/models';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../services';
@Component({
  selector: 'app-header',
  templateUrl: 'header.component.html',
  styleUrls: ['./header.component.scss'],
  standalone: true,
  imports: [DxToolbarModule, DxButtonModule, UserPanelComponent]
})
export class HeaderComponent {
  @Output()
  menuToggle = new EventEmitter<boolean>();

  @Input()
  menuToggleEnabled = false;

  @Input()
  title!: string;

  user: User;

  userMenuItems = [
    {
      text: 'Logout',
      icon: 'runner',
      onClick: () => {
        this._authService.logout();
      }
    }
  ];

  constructor(private _authService: AuthService) {
    this._authService.user$.pipe(takeUntilDestroyed()).subscribe((user: User) => (this.user = user));
  }

  toggleMenu = () => {
    this.menuToggle.emit();
  };
}
