import { Component } from '@angular/core';
import { AppInfoService } from '../../services';
import { DxToastModule } from 'devextreme-angular';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-footer',
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.scss'],
  standalone: true,
  imports: [DxToastModule]
})
export class FooterComponent {
  constructor(
    public appInfo: AppInfoService,
    public notificationService: NotificationService
  ) {}
}
