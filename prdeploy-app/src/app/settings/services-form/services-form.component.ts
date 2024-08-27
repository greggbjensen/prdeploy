import { Component, Input } from '@angular/core';
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
import _ from 'lodash';

@Component({
  selector: 'app-services-form',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatTableModule, MatInputModule, FormsModule, JsonPipe],
  templateUrl: './services-form.component.html',
  styleUrl: './services-form.component.scss'
})
export class ServicesFormComponent {
  displayColumns = ['name', 'path', 'remove'];
  hasServices = false;
  showOwner = false;
  bindingServiceList: ServiceSettings[] = [];
  actingServiceList: ServiceSettings[] = [];

  private _bindingLevel: SettingsLevel;

  private _services: OwnerRepoValueOfListOfServiceSettings;
  @Input() set services(value: OwnerRepoValueOfListOfServiceSettings) {
    this._services = value;
    this.fillServicesList();
  }

  get services() {
    return this._services;
  }

  private _level: SettingsLevel;
  @Input() set level(value: SettingsLevel) {
    this._level = value;
    this.showOwner = this.level == 'repo';
    this.fillServicesList();
  }

  get level() {
    return this._level;
  }

  constructor(private _dialog: MatDialog) {}

  async showAddDialog() {
    const dialogRef = this._dialog.open<AddServiceDialogComponent, void, string>(AddServiceDialogComponent, {
      width: '450px',
      height: '220px'
    });
    const service = await firstValueFrom(dialogRef.afterClosed());
    if (service) {
      this.add(service);
    }
  }

  add(serviceName: string) {
    // Do not add multiple of the same.
    if (this.actingServiceList.find(s => s.name === serviceName)) {
      return;
    }

    this.actingServiceList = _.sortBy(
      [{ name: serviceName, path: serviceName }, ...this.actingServiceList],
      s => s.name
    );

    this.saveServicesList();
  }

  remove(serviceName: any) {
    this.actingServiceList = this.actingServiceList.filter(s => s.name !== serviceName);
    this.saveServicesList();
  }

  private fillServicesList() {
    if (!this.services || !this._level) {
      return;
    }

    // Copy over values.
    this.hasServices = this.services[this.level].length > 0;
    this._bindingLevel = this.hasServices ? this._level : 'owner';
    this.actingServiceList = _.sortBy([...this.services[this._level]], s => s.name);
    this.bindingServiceList = _.sortBy([...this.services[this._bindingLevel]], s => s.name);
  }

  private saveServicesList() {
    if (!this.services || !this._level) {
      return;
    }

    // Copy over results.
    this._services[this._level] = this.actingServiceList;
    this.hasServices = this.actingServiceList.length > 0;
    this._bindingLevel = this.hasServices ? this._level : 'owner';
    if (this.hasServices) {
      this.bindingServiceList = [...this.actingServiceList];
    } else {
      this.bindingServiceList = [...this.services[this._bindingLevel]];
    }
  }
}
