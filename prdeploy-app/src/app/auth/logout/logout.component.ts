import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { AuthService } from 'src/app/shared/services';

@Component({
  templateUrl: './logout.component.html',
  styleUrls: ['./logout.component.scss'],
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: true
})
export class LogoutComponent implements OnInit {
  constructor(private authService: AuthService) {}

  size = '48px';

  ngOnInit() {
    this.authService.logout();
  }
}
