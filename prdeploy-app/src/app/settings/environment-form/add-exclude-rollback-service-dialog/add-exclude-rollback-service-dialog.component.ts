import { Component } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import {
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from '@angular/material/dialog';
import { DxTextBoxModule } from 'devextreme-angular';

@Component({
  selector: 'app-add-exclude-rollback-service-dialog',
  standalone: true,
  imports: [MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButtonModule, DxTextBoxModule],
  templateUrl: './add-exclude-rollback-service-dialog.component.html',
  styleUrl: './add-exclude-rollback-service-dialog.component.scss'
})
export class AddExcludeRollbackServiceDialogComponent {
  name = '';

  constructor(private _dialogRef: MatDialogRef<AddExcludeRollbackServiceDialogComponent>) {
    this._dialogRef
      .afterOpened()
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        this.clearFields();
      });
  }

  clearFields() {
    this.name = '';
  }

  add(): void {
    if (!this.name) {
      return;
    }

    this._dialogRef.close(this.name);
  }

  cancel(): void {
    this._dialogRef.close(null);
  }
}
