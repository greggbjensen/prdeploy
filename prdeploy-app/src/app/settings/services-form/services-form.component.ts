import { ChangeDetectorRef, Component, Input } from '@angular/core';
import { OwnerRepoValueOfListOfServiceSettings, ServiceSettings } from 'src/app/shared/graphql';
import { SettingsLevel } from '../models';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { AddServiceDialogComponent } from './add-service-dialog/add-service-dialog.component';
import { MatDialog } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';
import { JsonPipe } from '@angular/common';

@Component({
  selector: 'app-services-form',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatTableModule, MatInputModule, FormsModule, JsonPipe],
  templateUrl: './services-form.component.html',
  styleUrl: './services-form.component.scss'
})
export class ServicesFormComponent {
  bindingLevel: SettingsLevel;
  displayColumns = ['name', 'path', 'remove'];
  hasServices = false;
  showOwner = false;
  serviceList = new MatTableDataSource<ServiceSettings>();

  private _services: OwnerRepoValueOfListOfServiceSettings;
  @Input() set services(value: OwnerRepoValueOfListOfServiceSettings) {
    this._services = value;
    if (this._level && this.services) {
      this.serviceList.data = [...this.services[this._level]];
    }
    this.updateServicesList();
  }

  get services() {
    return this._services;
  }

  private _level: SettingsLevel;
  @Input() set level(value: SettingsLevel) {
    this._level = value;
    this.showOwner = this.level == 'repo';

    if (this._level && this.services) {
      this.serviceList.data = [...this.services[this._level]];
    }
    this.updateServicesList();
  }

  get level() {
    return this._level;
  }

  constructor(
    private _dialog: MatDialog,
    private _changeDectectorRef: ChangeDetectorRef
  ) {}

  async showAddDialog() {
    const dialogRef = this._dialog.open<AddServiceDialogComponent, void, string>(AddServiceDialogComponent, {
      width: '450px',
      height: '210px'
    });
    const service = await firstValueFrom(dialogRef.afterClosed());
    if (service) {
      this.add(service);
    }
  }

  add(serviceName: string) {
    // Do not add multiple of the same.
    if (this.serviceList.data.find(s => s.name === serviceName)) {
      return;
    }

    this.serviceList.data = [{ name: serviceName, path: serviceName }, ...this.serviceList.data];

    this.updateServicesList();
  }

  remove(serviceName: any) {
    this.serviceList.data = this.serviceList.data.filter(s => s.name !== serviceName);
    this.updateServicesList();
  }

  private updateServicesList() {
    if (!this.serviceList || !this._level) {
      return;
    }

    // Copy over results.
    this._services[this._level] = this.serviceList.data;
    this.hasServices = this.serviceList.data.length > 0;
    this.bindingLevel = this.hasServices ? this._level : 'owner';
    this._changeDectectorRef.detectChanges();
  }
}
