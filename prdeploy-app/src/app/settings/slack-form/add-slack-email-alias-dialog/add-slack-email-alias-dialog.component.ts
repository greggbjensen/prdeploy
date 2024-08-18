import { ChangeDetectorRef, Component, EventEmitter, Input, Output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { DxPopupModule } from 'devextreme-angular';
import { DxTextBoxModule } from 'devextreme-angular/ui/text-box';

@Component({
  selector: 'app-add-slack-email-alias-dialog',
  standalone: true,
  imports: [DxPopupModule, MatButtonModule, DxTextBoxModule],
  templateUrl: './add-slack-email-alias-dialog.component.html',
  styleUrl: './add-slack-email-alias-dialog.component.scss'
})
export class AddSlackEmailAliasDialogComponent {
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() addEmailAlias = new EventEmitter<string>();

  email = '';

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
    this.email = '';
  }

  onVisibleChange(): void {
    this.visibleChange.emit(this.visible);
  }

  add(): void {
    if (!this.email) {
      return;
    }

    this.addEmailAlias.emit(this.email);
    this.visible = false;
    this._changeDetectorRef.detectChanges();
  }

  cancel(): void {
    this.visible = false;
    this._changeDetectorRef.detectChanges();
  }
}
