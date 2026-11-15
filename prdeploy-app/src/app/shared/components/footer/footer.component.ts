import { Component, ChangeDetectionStrategy } from '@angular/core';
import { AppInfoService } from '../../services';

@Component({
  selector: 'app-footer',
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.scss'],
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: []
})
export class FooterComponent {
  constructor(public appInfo: AppInfoService) {}
}
