import { ChangeDetectorRef, Component, Input } from '@angular/core';
import { OwnerRepoValueOfListOfServiceSettings, ServiceSettings } from 'src/app/shared/graphql';
import { SettingsLevel } from '../models';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
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
  serviceList: ServiceSettings[] = [];

  private _services: OwnerRepoValueOfListOfServiceSettings;
  @Input() set services(value: OwnerRepoValueOfListOfServiceSettings) {
    this._services = value;
    this.updateServicesList(true);
  }

  get services() {
    return this._services;
  }

  private _level: SettingsLevel;
  @Input() set level(value: SettingsLevel) {
    this._level = value;
    this.showOwner = this.level == 'repo';
    this.updateServicesList(true);
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
    if (this.serviceList.find(s => s.name === serviceName)) {
      return;
    }

    this.serviceList.push({
      name: serviceName,
      path: serviceName
    });

    this.updateServicesList();
  }

  remove(serviceName: any) {
    const index = this.serviceList.findIndex(s => s.name === serviceName);
    if (index != -1) {
      this.serviceList.splice(index, 1);
    }
  }

  private updateServicesList(updateList = false) {
    if (!this.services || !this._level) {
      return;
    }

    this.bindingLevel = this.hasServices ? this._level : 'owner';
    if (updateList) {
      this.serviceList = [...this.services[this.bindingLevel]];
    } else {
      this.services[this.bindingLevel] = this.serviceList;
    }

    this.hasServices = this.services[this._level].length > 0;
    this._changeDectectorRef.detectChanges();
  }
}
