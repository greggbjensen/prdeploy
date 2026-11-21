import { Component, ChangeDetectionStrategy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogActions, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MtxButtonModule } from '@ng-matero/extensions/button';
import { EnvironmentUrlInput } from 'src/app/shared/graphql';

/** Dialog that collects the label and URL of a new environment link. */
@Component({
  selector: 'app-add-environment-url-dialog',
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatButtonModule,
    MtxButtonModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule
  ],
  templateUrl: './add-environment-url-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './add-environment-url-dialog.component.scss'
})
export class AddEnvironmentUrlDialogComponent {
  /** Label and URL fields; both are required and the URL must be an absolute http or https address. */
  form = new FormGroup({
    label: new FormControl('', Validators.required),
    url: new FormControl('', [Validators.required, Validators.pattern(/^https?:\/\/\S+$/i)])
  });

  /** Clears the form each time the dialog opens. */
  constructor(private _dialogRef: MatDialogRef<AddEnvironmentUrlDialogComponent, EnvironmentUrlInput | null>) {
    this._dialogRef
      .afterOpened()
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        this.clearFields();
      });
  }

  /** Resets both fields. */
  clearFields() {
    this.form.reset();
  }

  /** Closes the dialog with the trimmed label and URL. */
  add(): void {
    const { label, url } = this.form.value;
    this._dialogRef.close({ label: label!.trim(), url: url!.trim() });
  }

  /** Closes the dialog without adding anything. */
  cancel(): void {
    this._dialogRef.close(null);
  }
}
