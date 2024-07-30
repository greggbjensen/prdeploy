import { Component, OnInit } from '@angular/core';
import { Router, Navigation, ActivatedRoute } from '@angular/router';
import { AuthService } from 'src/app/shared/services';

@Component({
  selector: 'login',
  template: '',
  standalone: true
})
export class LoginComponent implements OnInit {
  private navigation: Navigation;
  returnUrl: string;
  loginError = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {
    this.navigation = this.router.getCurrentNavigation();
  }

  async ngOnInit(): Promise<void> {
    this.checkNavigationState();

    if (this.loginError) {
      this.redirectToLoginError();
      return;
    }

    this.loginWithAuth0();
  }

  private checkNavigationState() {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'];
    if (!this.navigation) {
      return;
    }
    if (!this.navigation.extras.state) {
      return;
    }

    this.returnUrl = this.navigation.extras.state['returnUrl'] || this.returnUrl;
    this.loginError = this.navigation.extras.state['loginError'];
  }

  public loginWithAuth0() {
    try {
      this.authService.login(this.returnUrl);
    } catch (error) {
      this.loginError = true;
      this.redirectToLoginError();
    }
  }

  private redirectToLoginError() {
    this.router.navigate(['/login/error'], { state: { loginError: this.loginError, skipUserCheck: true } });
  }
}
