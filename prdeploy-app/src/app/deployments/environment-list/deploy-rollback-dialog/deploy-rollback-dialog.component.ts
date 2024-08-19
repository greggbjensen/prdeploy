import { Component, DestroyRef, Inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from '@angular/material/dialog';
import { DxNumberBoxModule } from 'devextreme-angular';
import { firstValueFrom } from 'rxjs';
import { DeployEnvironmentRollbackGQL } from 'src/app/shared/graphql';
import { NotificationManager } from 'src/app/shared/managers';
import { LoggingService } from 'src/app/shared/services';
import { DeployRollbackDialogData } from './deploy-rollback-dialog-data';
import { DialogResult } from 'src/app/shared/models';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-deploy-rollback-dialog',
  standalone: true,
  imports: [MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButtonModule, DxNumberBoxModule],
  templateUrl: './deploy-rollback-dialog.component.html',
  styleUrl: './deploy-rollback-dialog.component.scss'
})
export class DeployRollbackDialogComponent {
  processing = false;
  rollbackCount = 1;

  constructor(
    private _deployEnvironmentDeployGQL: DeployEnvironmentRollbackGQL,
    private _notificationManager: NotificationManager,
    private _loggingService: LoggingService,
    private _destroyRef: DestroyRef,
    private _dialogRef: MatDialogRef<DeployRollbackDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: DeployRollbackDialogData
  ) {
    this._dialogRef
      .afterOpened()
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe(() => {
        this.clearFields();
      });
  }

  async rollback(): Promise<void> {
    this.processing = true;

    try {
      await firstValueFrom(
        this._deployEnvironmentDeployGQL.mutate({
          input: {
            owner: this.data.repository.owner,
            repo: this.data.repository.repo,
            environment: this.data.environment,
            pullNumber: this.data.pullNumber,
            count: this.rollbackCount > 1 ? this.rollbackCount : undefined
          }
        })
      );

      this._notificationManager.show(
        `Rollback ${this.data.environment} comment added, it may take a minute to update.`
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
    this.rollbackCount = 1;
    this.processing = false;
  }
}
