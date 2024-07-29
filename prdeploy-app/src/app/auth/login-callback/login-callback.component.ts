import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService as Auth0Service } from '@auth0/auth0-angular';
import { DxLoadIndicatorModule } from 'devextreme-angular';
import { take, takeWhile } from 'rxjs/operators';

@Component({
  selector: 'login-callback',
  templateUrl: './login-callback.component.html',
  styleUrls: ['./login-callback.component.scss'],
  standalone: true,
  imports: [DxLoadIndicatorModule]
})
export class LoginCallbackComponent implements OnInit {
  private handledCallback = false;

  constructor(
    private router: Router,
    private auth0Service: Auth0Service
  ) {}

  async ngOnInit() {
    this.auth0Service.appState$.pipe(take(1)).subscribe(() => {
      this.handledCallback = true;
    });
    await this.auth0Service.isLoading$.pipe(takeWhile(isLoading => isLoading)).toPromise();
    if (!this.handledCallback) {
      this.router.navigate(['/'], { replaceUrl: true });
    }
  }
}
