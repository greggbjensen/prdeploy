import { Component, Input, Output, EventEmitter } from '@angular/core';
import { UserPanelComponent } from '../user-panel/user-panel.component';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DxToolbarModule } from 'devextreme-angular/ui/toolbar';
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

  userMenuItems = [
    {
      text: 'Logout',
      icon: 'runner',
      onClick: () => {
        this._authService.logout();
      }
    }
  ];

  constructor(private _authService: AuthService) {}

  toggleMenu = () => {
    this.menuToggle.emit();
  };
}
