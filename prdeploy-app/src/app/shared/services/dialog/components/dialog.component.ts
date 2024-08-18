import { Component } from '@angular/core';
import { Subject } from 'rxjs';
import { DxPopupModule } from 'devextreme-angular';
import { SafeHtmlPipe } from 'src/app/shared/pipes';
import { DialogButton } from '../models/dialog-button';
import { MatButtonModule } from '@angular/material/button';

@Component({
  standalone: true,
  selector: 'nav-dialog',
  templateUrl: './dialog.component.html',
  styleUrls: ['./dialog.component.scss'],
  imports: [DxPopupModule, MatButtonModule, SafeHtmlPipe]
})
export class DialogComponent {
  private subject: Subject<string>;
  public visible = false;
  public result: string;
  public title: string;
  public content: string[];
  public hideOnOutsideClick = false;
  public buttons: DialogButton[];

  constructor() {}

  public show(title: string, content: string[], buttons: DialogButton[]): Subject<string> {
    this.result = null;
    this.title = title;
    this.content = content;
    this.buttons = buttons;
    this.visible = true;
    this.subject = new Subject();
    return this.subject;
  }

  public buttonClicked(text: string): void {
    this.result = text;
    this.visible = false;
  }

  public onHidden(): void {
    this.subject.next(this.result);
    this.subject.complete();
  }
}
