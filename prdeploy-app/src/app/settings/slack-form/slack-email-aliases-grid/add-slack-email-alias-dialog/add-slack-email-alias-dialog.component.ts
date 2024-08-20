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
import { DxTextBoxModule } from 'devextreme-angular/ui/text-box';

@Component({
  selector: 'app-add-slack-email-alias-dialog',
  standalone: true,
  imports: [MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButtonModule, DxTextBoxModule],
  templateUrl: './add-slack-email-alias-dialog.component.html',
  styleUrl: './add-slack-email-alias-dialog.component.scss'
})
export class AddSlackEmailAliasDialogComponent {
  email = '';

  constructor(private _dialogRef: MatDialogRef<AddSlackEmailAliasDialogComponent>) {
    this._dialogRef
      .afterOpened()
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        this.clearFields();
      });
  }

  clearFields() {
    this.email = '';
  }

  add(): void {
    if (!this.email) {
      return;
    }

    this._dialogRef.close(this.email);
  }

  cancel(): void {
    this._dialogRef.close(null);
  }
}
