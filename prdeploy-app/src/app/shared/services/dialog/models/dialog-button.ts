import { ButtonType } from 'devextreme/common';

export class DialogButton {
  constructor(
    public text: string,
    public type: ButtonType = 'normal',
    public automationId: string = null
  ) {}
}
