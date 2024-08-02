import { Injectable } from '@angular/core';
import { DialogComponent } from './components/dialog.component';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { DialogButton } from './models/dialog-button';
import { StatusDialogComponent } from './components/status-dialog.component';
import { StatusDialogType } from './models/status-dialog-type';
import DialogUtils from './dialog-utils';

@Injectable({
  providedIn: 'root'
})
export class DialogService {
  public dialogComponent: DialogComponent;
  public statusDialogComponent: StatusDialogComponent;

  public show(title: string, content: string[], buttons: string[]): Observable<string> {
    const dialogButtons: DialogButton[] = [];
    for (const button of buttons) {
      dialogButtons.push(new DialogButton(button));
    }
    return this.showDialog(title, content, dialogButtons);
  }

  public hide(): void {
    this.dialogComponent.visible = false;
  }

  public showDialog(title: string, content: string[], buttons: DialogButton[]): Observable<string> {
    return this.dialogComponent.show(title, content, buttons);
  }

  public showStatusDialog(
    type: StatusDialogType,
    title: string,
    content: string[],
    buttons: DialogButton[],
    errorMessages: string[] = [],
    showDoNotShowAgainCheckBox: boolean = false,
    id: string = null
  ): Observable<string> {
    if (this.isMarkedDoNotShowAgain(id)) {
      return of('doNotShow');
    }

    return this.statusDialogComponent.show(
      type,
      title,
      content,
      buttons,
      errorMessages,
      showDoNotShowAgainCheckBox,
      id
    );
  }

  public getYesNo(title: string, content: string[], defaultResult: boolean = false): Observable<boolean> {
    const buttons = [new DialogButton('No', 'normal'), new DialogButton('Yes', 'default')];
    return this.dialogComponent
      .show(title, content, buttons)
      .pipe(map(result => (result === null ? defaultResult : result === 'Yes')));
  }

  public getContinueCancel(title: string, content: string[], defaultResult: boolean = false): Observable<boolean> {
    const buttons = [new DialogButton('Cancel', 'normal'), new DialogButton('Continue', 'default')];
    return this.dialogComponent
      .show(title, content, buttons)
      .pipe(map(result => (result === null ? defaultResult : result === 'Continue')));
  }

  public getNoCancel(title: string, content: string[], defaultResult: boolean = false): Observable<boolean> {
    const buttons = [new DialogButton('No', 'normal'), new DialogButton('Save', 'default')];
    return this.dialogComponent
      .show(title, content, buttons)
      .pipe(map(result => (result === null ? defaultResult : result === 'Save')));
  }

  public getSaveCancel(title: string, content: string[], defaultResult: boolean = false): Observable<boolean> {
    const buttons = [new DialogButton('Cancel', 'normal'), new DialogButton('Save', 'default')];
    return this.dialogComponent
      .show(title, content, buttons)
      .pipe(map(result => (result === null ? defaultResult : result === 'Save')));
  }

  private isMarkedDoNotShowAgain(id: string): boolean {
    if (id) {
      const value = localStorage.getItem(DialogUtils.getDoNotShowPrefix + id);
      if (value && value == 'true') {
        return true;
      }
    }
    return false;
  }
}
