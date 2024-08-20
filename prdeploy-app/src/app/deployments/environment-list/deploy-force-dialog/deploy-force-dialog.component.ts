import { AfterViewInit, Component, DestroyRef, Inject, OnInit } from '@angular/core';
import { debounceTime, firstValueFrom } from 'rxjs';
import { DeployEnvironmentDeployGQL, OpenPullRequestsGQL, PullRequest } from 'src/app/shared/graphql';
import { LoggingService } from 'src/app/shared/services';
import {
  MAT_DIALOG_DATA,
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from '@angular/material/dialog';
import { NotificationManager } from 'src/app/shared/managers';
import { DialogResult } from 'src/app/shared/models';
import { MatButtonModule } from '@angular/material/button';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DeployForceDialogData } from './deploy-force-dialog-data';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatInputModule } from '@angular/material/input';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import _ from 'lodash';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-deploy-force-dialog',
  templateUrl: './deploy-force-dialog.component.html',
  styleUrls: ['./deploy-force-dialog.component.scss'],
  standalone: true,
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    MatAutocompleteModule,
    MatInputModule,
    MatIconModule,
    MatCheckboxModule,
    MatButtonModule,
    MatFormFieldModule,
    ReactiveFormsModule
  ]
})
export class DeployForceDialogComponent implements OnInit, AfterViewInit {
  processing = false;

  form = new FormGroup({
    pullRequest: new FormControl('', [Validators.required]),
    retainLocks: new FormControl(false)
  });

  selectedPullRequest: PullRequest;
  openPullRequests: PullRequest[];

  constructor(
    private _openPullRequestsGQL: OpenPullRequestsGQL,
    private _deployEnvironmentDeployGQL: DeployEnvironmentDeployGQL,
    private _notificationManager: NotificationManager,
    private _loggingService: LoggingService,
    private _dialogRef: MatDialogRef<DeployForceDialogComponent>,
    private _destroyRef: DestroyRef,
    @Inject(MAT_DIALOG_DATA) public data: DeployForceDialogData
  ) {
    this._dialogRef
      .afterOpened()
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        this.clearFields();
      });
  }

  ngAfterViewInit(): void {
    this.form.controls.pullRequest.valueChanges
      .pipe(takeUntilDestroyed(this._destroyRef), debounceTime(300))
      .subscribe(value => {
        if (!_.isObject(value)) {
          this.filterPullRequests(value);
        }
      });
  }

  ngOnInit(): void {
    this.filterPullRequests();
  }

  async filterPullRequests(search: string = '') {
    const result = await firstValueFrom(
      this._openPullRequestsGQL.fetch({
        input: {
          owner: this.data.repository.owner,
          repo: this.data.repository.repo,
          search
        }
      })
    );

    this.openPullRequests = result.data.openPullRequests;
  }

  formatPullRequest(item: PullRequest) {
    return item && item.number ? `#${item.number}  ${item.title}  (${item.user?.name})` : '';
  }

  selectPullRequest(event: MatAutocompleteSelectedEvent) {
    this.selectedPullRequest = event.option.value;
  }

  async forceDeploy(): Promise<void> {
    this.processing = true;

    try {
      await firstValueFrom(
        this._deployEnvironmentDeployGQL.mutate({
          input: {
            owner: this.data.repository.owner,
            repo: this.data.repository.repo,
            environment: this.data.environment,
            pullNumber: this.selectedPullRequest.number,
            force: true,
            retain: this.form.value.retainLocks
          }
        })
      );

      this._notificationManager.show(
        `Force deploy ${this.data.environment} comment added, it may take a minute to update.`
      );
      this._dialogRef.close(DialogResult.Save);
    } catch (error) {
      this._loggingService.error(error);
    }

    this.processing = false;
  }

  cancel(): void {
    this._dialogRef.close(DialogResult.Cancel);
  }

  private clearFields() {
    this.selectedPullRequest = null;
    this.processing = false;
    this.form.reset();
  }
}
