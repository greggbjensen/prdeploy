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
  selector: 'app-add-environment-dialog',
  standalone: true,
  imports: [MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButtonModule, DxTextBoxModule],
  templateUrl: './add-environment-dialog.component.html',
  styleUrl: './add-environment-dialog.component.scss'
})
export class AddEnvironmentDialogComponent {
  name = '';

  constructor(private _dialogRef: MatDialogRef<AddEnvironmentDialogComponent>) {
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
