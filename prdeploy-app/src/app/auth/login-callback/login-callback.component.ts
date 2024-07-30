import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { DxLoadIndicatorModule } from 'devextreme-angular';
import { AuthService } from 'src/app/shared/services';

@Component({
  selector: 'login-callback',
  templateUrl: './login-callback.component.html',
  styleUrls: ['./login-callback.component.scss'],
  standalone: true,
  imports: [DxLoadIndicatorModule]
})
export class LoginCallbackComponent implements OnInit {
  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  async ngOnInit() {
    // this.authService.handleCallbackRedirect();
    console.log(`callback`);
  }
}
