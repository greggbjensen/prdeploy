import { Component, DestroyRef, Inject, ViewChild } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { DeployEnvironmentDeployGQL, OpenPullRequestsGQL, PullRequest } from 'src/app/shared/graphql';
import { LoggingService } from 'src/app/shared/services';
import { DxCheckBoxModule, DxSelectBoxComponent, DxSelectBoxModule } from 'devextreme-angular';
import {
  MAT_DIALOG_DATA,
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from '@angular/material/dialog';
import CustomStore from 'devextreme/data/custom_store';
import { NotificationManager } from 'src/app/shared/managers';
import { DialogResult } from 'src/app/shared/models';
import { MatButtonModule } from '@angular/material/button';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DeployForceDialogData } from './deploy-force-dialog-data';

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
    DxSelectBoxModule,
    MatButtonModule,
    DxCheckBoxModule
  ]
})
export class DeployForceDialogComponent {
  @ViewChild('selectPullRequest') selectPullRequestComponent: DxSelectBoxComponent;

  processing = false;
  retainLocks = false;

  selectedPullRequest: PullRequest;
  openPullRequests: CustomStore<PullRequest, number>;

  constructor(
    private _openPullRequestsGQL: OpenPullRequestsGQL,
    private _deployEnvironmentDeployGQL: DeployEnvironmentDeployGQL,
    private _notificationManager: NotificationManager,
    private _loggingService: LoggingService,
    private _destroyRef: DestroyRef,
    private _dialogRef: MatDialogRef<DeployForceDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: DeployForceDialogData
  ) {
    this._dialogRef
      .afterOpened()
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe(() => {
        this.clearFields();
      });

    this.openPullRequests = new CustomStore<PullRequest, number>({
      key: 'number',
      load: async options => {
        const result = await firstValueFrom(
          this._openPullRequestsGQL.fetch({
            input: {
              owner: this.data.repository.owner,
              repo: this.data.repository.repo,
              search: options.searchValue
            }
          })
        );
        return result.data.openPullRequests;
      }
    });
  }

  pullRequestDisplayExpr(item: PullRequest) {
    return item ? `#${item.number}  ${item.title}  (${item.user?.name})` : '';
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
            retain: this.retainLocks
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
    this.retainLocks = false;
    if (this.selectPullRequestComponent) {
      this.selectPullRequestComponent.value = null;
    }
  }
}
