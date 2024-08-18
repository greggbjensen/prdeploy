import { ChangeDetectorRef, Component, EventEmitter, Input, Output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { DxPopupModule, DxTextBoxModule } from 'devextreme-angular';

@Component({
  selector: 'app-add-environment-dialog',
  standalone: true,
  imports: [DxPopupModule, MatButtonModule, DxTextBoxModule],
  templateUrl: './add-environment-dialog.component.html',
  styleUrl: './add-environment-dialog.component.scss'
})
export class AddEnvironmentDialogComponent {
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() addEnvironment = new EventEmitter<string>();

  name = '';

  private _visible = false;

  get visible() {
    return this._visible;
  }

  @Input()
  set visible(value: boolean) {
    this._visible = value;
    this.clearFields();
  }

  constructor(private _changeDetectorRef: ChangeDetectorRef) {}

  clearFields() {
    this.name = '';
  }

  onVisibleChange(): void {
    this.visibleChange.emit(this.visible);
  }

  add(): void {
    if (!this.name) {
      return;
    }

    this.addEnvironment.emit(this.name);
    this.visible = false;
    this._changeDetectorRef.detectChanges();
  }

  cancel(): void {
    this.visible = false;
    this._changeDetectorRef.detectChanges();
  }
}
