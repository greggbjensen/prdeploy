import { Component, Inject, ChangeDetectionStrategy } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogActions,
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
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MtxButtonModule } from '@ng-matero/extensions/button';

@Component({
  selector: 'app-add-repo-dialog',
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MtxButtonModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule
  ],
  templateUrl: './add-repo-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './add-repo-dialog.component.scss'
})
export class AddRepoDialogComponent {
  processing = false;

  /** Reason the last add attempt was rejected by the API, shown in the dialog until the form changes. */
  errorMessage: string | null = null;

  form = new FormGroup({
    owner: new FormControl('', Validators.required),
    repo: new FormControl('', Validators.required)
  });

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

    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.errorMessage = null;
    });
  }

  clearFields() {
    this.errorMessage = null;
    this.form.reset();
    this.form.patchValue({
      owner: this.data.owner || ''
    });
  }

  async add(): Promise<void> {
    if (!this.form.valid) {
      return;
    }

    this.processing = true;
    this.errorMessage = null;

    try {
      const owner = this.form.value.owner;
      const repo = this.form.value.repo;
      const repository: Repository = {
        owner,
        repo
      };
      const response = await firstValueFrom(
        this._ownerRepoAddEnabledGQL.mutate({
          variables: {
            input: repository
          }
        })
      );

      // The API rejects repositories the user is not a contributor on with a reason; keep the dialog open.
      const result = response.data?.ownerRepoAddEnabled;
      if (!result?.success) {
        this.errorMessage = result?.message || `Unable to add repository ${owner}/${repo}.`;
        this._notificationManager.show(this.errorMessage, 'danger');
        this.processing = false;
        return;
      }

      this._notificationManager.show(`Repository ${owner}/${repo} added.`);
      this._dialogRef.close(repository);
    } catch (error) {
      this._loggingService.error(error, `Error adding repository.`);
      this._notificationManager.show('Error adding repository.', 'danger');
    }

    this.processing = false;
  }

  cancel(): void {
    this._dialogRef.close(null);
  }
}
