import { Component } from '@angular/core';
import { AppInfoService } from '../../services';

@Component({
  selector: 'app-footer',
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.scss'],
  standalone: true
})
export class FooterComponent {
  constructor(public appInfo: AppInfoService) {}
}
