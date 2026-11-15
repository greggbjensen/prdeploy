import { Component, ChangeDetectionStrategy } from '@angular/core';
import { AuthService } from 'src/app/shared/services';

@Component({
  selector: 'login-error',
  templateUrl: './login-error.component.html',
  styleUrls: ['./login-error.component.scss'],
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: true
})
export class LoginErrorComponent {
  loggingOut = false;

  constructor(private authService: AuthService) {}

  logInAgainClicked() {
    this.loggingOut = true;
    this.authService.logout();
  }
}
