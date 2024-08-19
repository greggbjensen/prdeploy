import { Component, HostBinding } from '@angular/core';
import { ScreenService, AppInfoService } from './shared/services';
import { RouterOutlet } from '@angular/router';
import { SideNavOuterToolbarComponent, FooterComponent } from './shared/components';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  standalone: true,
  imports: [RouterOutlet, SideNavOuterToolbarComponent, FooterComponent]
})
export class AppComponent {
  @HostBinding('class') get getClass() {
    return Object.keys(this.screen.sizes)
      .filter(cl => this.screen.sizes[cl])
      .join(' ');
  }

  constructor(
    private screen: ScreenService,
    public appInfo: AppInfoService
  ) {}
}
