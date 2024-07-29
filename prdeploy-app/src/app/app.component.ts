import { AfterViewInit, Component, HostBinding, ViewChild } from '@angular/core';
import {
  ScreenService,
  AppInfoService,
  DialogComponent,
  StatusDialogComponent,
  DialogService
} from './shared/services';
import { RouterOutlet } from '@angular/router';
import { SideNavOuterToolbarComponent, FooterComponent } from './shared/components';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  standalone: true,
  imports: [RouterOutlet, SideNavOuterToolbarComponent, FooterComponent, DialogComponent, StatusDialogComponent]
})
export class AppComponent implements AfterViewInit {
  @HostBinding('class') get getClass() {
    return Object.keys(this.screen.sizes)
      .filter(cl => this.screen.sizes[cl])
      .join(' ');
  }

  @ViewChild(DialogComponent) dialogComponent: DialogComponent;
  @ViewChild(StatusDialogComponent) statusDialogComponent: StatusDialogComponent;

  constructor(
    private screen: ScreenService,
    public appInfo: AppInfoService,
    private _dialogService: DialogService
  ) {}

  ngAfterViewInit() {
    // TODO GBJ: We should be able to dynamically create these.
    this._dialogService.dialogComponent = this.dialogComponent;
    this._dialogService.statusDialogComponent = this.statusDialogComponent;
  }
}
