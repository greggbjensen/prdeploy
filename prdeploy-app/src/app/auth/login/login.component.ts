import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { DxButtonModule } from 'devextreme-angular';
import { AuthService } from 'src/app/shared/services';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [RouterModule, DxButtonModule],
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
