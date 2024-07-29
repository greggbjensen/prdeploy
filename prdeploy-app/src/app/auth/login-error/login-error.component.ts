import { Component } from '@angular/core';
import { DxButtonModule } from 'devextreme-angular';
import { AuthService } from 'src/app/shared/services';

@Component({
  selector: 'login-error',
  templateUrl: './login-error.component.html',
  styleUrls: ['./login-error.component.scss'],
  standalone: true,
  imports: [DxButtonModule]
})
export class LoginErrorComponent {
  loggingOut = false;

  constructor(private authService: AuthService) {}

  logInAgainClicked() {
    this.loggingOut = true;
    this.authService.logout(false);
  }

  contactUsClicked() {
    window.open('https://www.selleractive.com/support/kb-tickets/new');
  }
}
