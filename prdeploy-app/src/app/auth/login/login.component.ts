import { Component, OnInit } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from 'src/app/shared/services';
import { Location } from '@angular/common';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [RouterModule, MatIconModule, MatButtonModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit {
  visible = false;
  forbidden = false;
  constructor(
    private _activatedRoute: ActivatedRoute,
    private _router: Router,
    private _location: Location,
    authService: AuthService
  ) {
    const path = this._location.path();
    if (authService.hasValidToken() && path.startsWith('/login')) {
      this._router.navigate(['/auth/secure-redirect']);
    } else {
      this.visible = true;
    }
  }

  ngOnInit(): void {
    this.forbidden = this._activatedRoute.snapshot.queryParams['forbidden'] == 'true';
  }
}
