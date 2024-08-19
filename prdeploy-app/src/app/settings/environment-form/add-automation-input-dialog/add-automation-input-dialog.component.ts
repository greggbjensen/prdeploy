import { Component, DestroyRef } from '@angular/core';
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
  selector: 'app-add-automation-input-dialog',
  standalone: true,
  imports: [MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButtonModule, DxTextBoxModule],
  templateUrl: './add-automation-input-dialog.component.html',
  styleUrl: './add-automation-input-dialog.component.scss'
})
export class AddAutomationInputDialogComponent {
  name = '';

  constructor(
    private _destroyRef: DestroyRef,
    private _dialogRef: MatDialogRef<AddAutomationInputDialogComponent>
  ) {
    this._dialogRef
      .afterOpened()
      .pipe(takeUntilDestroyed(this._destroyRef))
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
