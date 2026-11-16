import { Component, ChangeDetectionStrategy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogActions, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-add-slack-email-alias-dialog',
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule
  ],
  templateUrl: './add-slack-email-alias-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './add-slack-email-alias-dialog.component.scss'
})
export class AddSlackEmailAliasDialogComponent {
  form = new FormGroup({
    email: new FormControl('', Validators.required)
  });

  constructor(private _dialogRef: MatDialogRef<AddSlackEmailAliasDialogComponent>) {
    this._dialogRef
      .afterOpened()
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        this.clearFields();
      });
  }

  clearFields() {
    this.form.reset();
  }

  add(): void {
    this._dialogRef.close(this.form.value.email);
  }

  cancel(): void {
    this._dialogRef.close(null);
  }
}
