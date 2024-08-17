import { Component, DestroyRef, Input } from '@angular/core';
import { DxListModule } from 'devextreme-angular/ui/list';
import { DxContextMenuModule } from 'devextreme-angular/ui/context-menu';
import { AuthService } from '../../services';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { User } from '../../models';

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

  user: User = null;

  constructor(
    private _authService: AuthService,
    private _destroyRef: DestroyRef
  ) {
    this._authService.user$.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(user => (this.user = user));
  }
}
