export enum DialogButtonTypes {
  Primary = 'primary',
  Secondary = 'secondary',
  Tertiary = 'tertiary',
  Danger = 'danger'
}

export class DialogButton {
  constructor(
    public text: string,
    public type: `${DialogButtonTypes}` = DialogButtonTypes.Secondary,
    public url: string = null,
    public automationId: string = null
  ) {}
}
