import { Component, Inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';
import { DeployEnvironmentRollbackGQL } from 'src/app/shared/graphql';
import { NotificationManager } from 'src/app/shared/managers';
import { LoggingService } from 'src/app/shared/services';
import { DeployRollbackDialogData } from './deploy-rollback-dialog-data';
import { DialogResult } from 'src/app/shared/models';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatInputModule } from '@angular/material/input';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-deploy-rollback-dialog',
  standalone: true,
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    MatButtonModule,
    MatInputModule,
    ReactiveFormsModule
  ],
  templateUrl: './deploy-rollback-dialog.component.html',
  styleUrl: './deploy-rollback-dialog.component.scss'
})
export class DeployRollbackDialogComponent {
  form = new FormGroup({
    rollbackCount: new FormControl(1)
  });

  processing = false;

  constructor(
    private _deployEnvironmentDeployGQL: DeployEnvironmentRollbackGQL,
    private _notificationManager: NotificationManager,
    private _loggingService: LoggingService,
    private _dialogRef: MatDialogRef<DeployRollbackDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: DeployRollbackDialogData
  ) {
    this._dialogRef
      .afterOpened()
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        this.clearFields();
      });
  }

  async rollback(): Promise<void> {
    this.processing = true;

    try {
      const rollbackCount = this.form.value.rollbackCount;
      await firstValueFrom(
        this._deployEnvironmentDeployGQL.mutate({
          input: {
            owner: this.data.repository.owner,
            repo: this.data.repository.repo,
            environment: this.data.environment,
            pullNumber: this.data.pullNumber,
            count: rollbackCount
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
    this.form.reset({
      rollbackCount: 1
    });
    this.processing = false;
  }
}
