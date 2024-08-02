import { Component } from '@angular/core';
import { DxLoadIndicatorModule } from 'devextreme-angular';

@Component({
  selector: 'login-callback',
  templateUrl: './login-callback.component.html',
  styleUrls: ['./login-callback.component.scss'],
  standalone: true,
  imports: [DxLoadIndicatorModule]
})
export class LoginCallbackComponent {}
