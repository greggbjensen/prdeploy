import { Component } from '@angular/core';
import { Subject } from 'rxjs';
import DialogUtils from '../dialog-utils';
import { DxButtonModule, DxCheckBoxModule, DxPopupModule } from 'devextreme-angular';
import { StatusDialogType } from '../models/status-dialog-type';
import { DialogButton } from '../models/dialog-button';

import { SafeHtmlPipe } from 'src/app/shared/pipes';

@Component({
  standalone: true,
  selector: 'status-dialog',
  templateUrl: './status-dialog.component.html',
  styleUrls: ['./status-dialog.component.scss'],
  imports: [DxPopupModule, DxCheckBoxModule, DxButtonModule, SafeHtmlPipe]
})
export class StatusDialogComponent {
  private subject: Subject<string>;
  public type: StatusDialogType = StatusDialogType.Success;
  public visible = false;
  public result: string;
  public title: string;
  public content: string[];
  public errorMessages: string[];
  public hideOnOutsideClick = false;
  public buttons: DialogButton[];
  public showDoNotShowAgainCheckBox: boolean;

  StatusDialogType = StatusDialogType;
  public doNotShowAgain: boolean = false;
  private id: string;

  constructor() {}

  public show(
    type: StatusDialogType,
    title: string,
    content: string[],
    buttons: DialogButton[],
    errorMessages: string[] = [],
    showDoNotShowAgainCheckBox: boolean = false,
    id: string = null
  ): Subject<string> {
    this.result = null;
    this.type = type;
    this.title = title;
    this.content = content;
    this.buttons = buttons;
    this.errorMessages = errorMessages;
    this.subject = new Subject();
    this.showDoNotShowAgainCheckBox = showDoNotShowAgainCheckBox;
    this.id = id;

    this.visible = true;

    return this.subject;
  }

  public buttonClicked(text: string): void {
    this.result = text;
    this.visible = false;

    if (this.doNotShowAgain == true) {
      this.markAsDoNotShowAgain();
    }
  }

  public onHidden(): void {
    this.subject.next(this.result);
    this.subject.complete();
  }

  private markAsDoNotShowAgain() {
    if (this.id) {
      localStorage.setItem(DialogUtils.getDoNotShowPrefix + this.id, 'true');
    }
  }
}
