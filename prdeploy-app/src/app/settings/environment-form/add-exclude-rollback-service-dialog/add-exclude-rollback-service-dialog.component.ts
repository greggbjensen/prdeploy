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
  selector: 'app-add-exclude-rollback-service-dialog',
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
  templateUrl: './add-exclude-rollback-service-dialog.component.html',
  styleUrl: './add-exclude-rollback-service-dialog.component.scss'
})
export class AddExcludeRollbackServiceDialogComponent {
  form = new FormGroup({
    name: new FormControl('', Validators.required)
  });

  constructor(private _dialogRef: MatDialogRef<AddExcludeRollbackServiceDialogComponent>) {
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
    this._dialogRef.close(this.form.value.name);
  }

  cancel(): void {
    this._dialogRef.close(null);
  }
}
