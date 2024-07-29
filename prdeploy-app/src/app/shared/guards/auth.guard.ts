import { Injectable } from '@angular/core';
import { Router, UrlTree } from '@angular/router';
import { AuthService } from 'src/app/shared/services';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard {
  constructor(
    private _router: Router,
    private _authService: AuthService
  ) {}

  async canActivate(): Promise<boolean | UrlTree> {
    if (this.skipCheck()) {
      return true;
    }

    const authenticated = await this._authService.ensureAuthenticated();
    if (!authenticated) {
      this._router.navigate(['forbidden']);
    }

    return authenticated;
  }

  private skipCheck(): boolean {
    return true; // TODO: Update once auth is in place
    // const navigation = this._router.getCurrentNavigation();
    // return navigation && navigation.extras.state && navigation.extras.state['skipUserCheck'];
  }
}
