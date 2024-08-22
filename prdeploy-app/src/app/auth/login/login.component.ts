import { Component, OnInit } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from 'src/app/shared/services';

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
    authService: AuthService
  ) {
    if (authService.hasValidToken()) {
      this._router.navigate(['/deployments']);
    } else {
      this.visible = true;
    }
  }

  ngOnInit(): void {
    this.forbidden = this._activatedRoute.snapshot.queryParams['forbidden'] == 'true';
  }
}
