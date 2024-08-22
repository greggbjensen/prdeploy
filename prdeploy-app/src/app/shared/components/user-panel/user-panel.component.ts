import { Component } from '@angular/core';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../services';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { User } from '../../models';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-user-panel',
  templateUrl: 'user-panel.component.html',
  styleUrls: ['./user-panel.component.scss'],
  standalone: true,
  imports: [MatMenuModule, MatButtonModule, MatIconModule]
})
export class UserPanelComponent {
  user: User = null;

  constructor(private _authService: AuthService) {
    this._authService.user$.pipe(takeUntilDestroyed()).subscribe(user => (this.user = user));
  }

  async logout() {
    this._authService.logout();
  }
}
