import { Component, Inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { DxTextBoxModule } from 'devextreme-angular';
import {
  MAT_DIALOG_DATA,
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';
import { OwnerRepoAddEnabledGQL } from 'src/app/shared/graphql';
import { NotificationManager } from 'src/app/shared/managers';
import { Repository } from 'src/app/shared/models';
import { LoggingService } from 'src/app/shared/services';
import { AddRepoDialogData } from './add-repo-dialog-data';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-add-repo-dialog',
  standalone: true,
  imports: [MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, DxTextBoxModule, MatButtonModule],
  templateUrl: './add-repo-dialog.component.html',
  styleUrl: './add-repo-dialog.component.scss'
})
export class AddRepoDialogComponent {
  repo: string;
  processing = false;

  constructor(
    private _ownerRepoAddEnabledGQL: OwnerRepoAddEnabledGQL,
    private _notificationManager: NotificationManager,
    private _loggingService: LoggingService,
    private _dialogRef: MatDialogRef<AddRepoDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: AddRepoDialogData
  ) {
    this._dialogRef
      .afterOpened()
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        this.clearFields();
      });
  }

  clearFields() {
    this.repo = '';
  }

  async add(): Promise<void> {
    if (!this.data.owner && this.repo) {
      return;
    }

    this.processing = true;

    try {
      const repository: Repository = {
        owner: this.data.owner,
        repo: this.repo
      };
      await firstValueFrom(
        this._ownerRepoAddEnabledGQL.mutate({
          input: repository
        })
      );

      this._notificationManager.show(`Repository ${this.data.owner}/${this.repo} added.`);
      this._dialogRef.close(repository);
    } catch (error) {
      this._loggingService.error(error, `Error adding repository.`);
      this._notificationManager.show('Error adding repository.', 'error');
    }

    this.processing = false;
  }

  cancel(): void {
    this._dialogRef.close(null);
  }
}
