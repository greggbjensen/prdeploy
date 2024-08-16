import { ChangeDetectorRef, Component, EventEmitter, Input, Output } from '@angular/core';
import { DxButtonModule, DxPopupModule, DxTextBoxModule } from 'devextreme-angular';

@Component({
  selector: 'app-add-exclude-rollback-service-dialog',
  standalone: true,
  imports: [DxPopupModule, DxButtonModule, DxTextBoxModule],
  templateUrl: './add-exclude-rollback-service-dialog.component.html',
  styleUrl: './add-exclude-rollback-service-dialog.component.scss'
})
export class AddExcludeRollbackServiceDialogComponent {
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() addInput = new EventEmitter<string>();

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

    this.addInput.emit(this.name);
    this.visible = false;
    this._changeDetectorRef.detectChanges();
  }

  cancel(): void {
    this.visible = false;
    this._changeDetectorRef.detectChanges();
  }
}
