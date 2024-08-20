import { Component } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import {
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MtxButtonModule } from '@ng-matero/extensions/button';

@Component({
  selector: 'app-add-slack-email-alias-dialog',
  standalone: true,
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    MatButtonModule,
    MtxButtonModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule
  ],
  templateUrl: './add-slack-email-alias-dialog.component.html',
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
