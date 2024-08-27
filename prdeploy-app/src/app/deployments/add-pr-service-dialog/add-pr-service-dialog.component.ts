import { KeyValuePipe } from '@angular/common';
import { Component, ViewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxChange, MatCheckboxModule } from '@angular/material/checkbox';
import {
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MtxButtonModule } from '@ng-matero/extensions/button';
import { firstValueFrom } from 'rxjs';
import {
  OpenPullRequestsGQL,
  PullRequest,
  PullRequestAddServicesGQL,
  RepositoryServicesGQL
} from 'src/app/shared/graphql';
import { NotificationManager, RepoManager } from 'src/app/shared/managers';
import { DialogResult } from 'src/app/shared/models';
import { LoggingService } from 'src/app/shared/services';
import { AlertPanelComponent } from 'src/app/shared/components';
import { MatListModule, MatSelectionList } from '@angular/material/list';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-add-pr-service-dialog',
  standalone: true,
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    MatButtonModule,
    MatAutocompleteModule,
    MatInputModule,
    MatIconModule,
    MatFormFieldModule,
    MatListModule,
    MatCheckboxModule,
    MtxButtonModule,
    ReactiveFormsModule,
    AlertPanelComponent,
    RouterModule,
    KeyValuePipe
  ],
  templateUrl: './add-pr-service-dialog.component.html',
  styleUrl: './add-pr-service-dialog.component.scss'
})
export class AddPrServiceDialogComponent {
  @ViewChild(MatSelectionList, { static: false }) listView: MatSelectionList;

  form = new FormGroup({
    pullRequest: new FormControl('', [Validators.required])
  });

  Object = Object;
  selectedPullRequest: PullRequest;
  openPullRequests: PullRequest[];
  repositoryServices: string[];
  selectedServices: string[] = [];
  processing = false;
  loadingServices = true;

  constructor(
    public repoManager: RepoManager,
    private _openPullRequestsGQL: OpenPullRequestsGQL,
    private _pullRequestAddServicesGQL: PullRequestAddServicesGQL,
    private _repositoryServicesGQL: RepositoryServicesGQL,
    private _notificationManager: NotificationManager,
    private _dialogRef: MatDialogRef<AddPrServiceDialogComponent>,
    private _loggingService: LoggingService
  ) {
    this._dialogRef
      .afterOpened()
      .pipe(takeUntilDestroyed())
      .subscribe(async () => {
        this.clearFields();

        this.loadingServices = true;
        const response = await firstValueFrom(
          this._repositoryServicesGQL.fetch({ input: { owner: this.repoManager.owner, repo: this.repoManager.repo } })
        );
        this.repositoryServices = response.data.repositoryServices;
        this.loadingServices = false;
      });

    this.filterPullRequests();

    this.form.controls.pullRequest.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(value => this.filterPullRequests(value));
  }

  async filterPullRequests(search: string = '') {
    const result = await firstValueFrom(
      this._openPullRequestsGQL.fetch({
        input: {
          owner: this.repoManager.owner,
          repo: this.repoManager.repo,
          search
        }
      })
    );

    this.openPullRequests = result.data.openPullRequests;
  }

  toggleServiceSelected(event: MatCheckboxChange, service: string) {
    if (event.checked) {
      this.selectedServices.push(service);
    } else {
      const index = this.selectedServices.indexOf(service);
      this.selectedServices.splice(index, 1);
    }
  }

  formatPullRequest(item: PullRequest) {
    return item && item.number ? `#${item.number}  ${item.title}  (${item.user?.name})` : '';
  }

  selectPullRequest(event: MatAutocompleteSelectedEvent) {
    this.selectedPullRequest = event.option.value;
  }

  clearPullRequest(event: MouseEvent) {
    event.stopPropagation();
    this.selectedPullRequest = null;
    this.form.controls.pullRequest.reset();
  }

  async addServicesToPr(): Promise<void> {
    this.processing = true;

    try {
      await firstValueFrom(
        this._pullRequestAddServicesGQL.mutate({
          input: {
            owner: this.repoManager.owner,
            repo: this.repoManager.repo,
            pullNumber: this.selectedPullRequest.number,
            services: this.selectedServices
          }
        })
      );

      this._notificationManager.show(`Add services comment added, it may take a minute to update.`);
      this._dialogRef.close(DialogResult.Save);
    } catch (error) {
      this._loggingService.error(error);
    }

    this.processing = false;
  }

  onDropDownBoxValueChanged(): void {
    this.updateListSelection();
  }

  onSelectedServicesChange(): void {
    this.selectedServices = this.listView.options.filter(o => o.selected).map(o => o.value);
  }

  updateListSelection() {
    if (!this.listView) {
      return;
    }

    if (!this.selectedServices || this.selectedServices.length === 0) {
      this.selectedServices = [];
    }
  }

  cancel(): void {
    this._dialogRef.close(DialogResult.Cancel);
  }

  private clearFields() {
    this.selectedPullRequest = null;
    this.processing = false;
    this.selectedServices = [];
    this.updateListSelection();
    this.form.controls.pullRequest.reset();
  }
}
