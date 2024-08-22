import { AfterViewInit, Directive, ElementRef } from '@angular/core';

@Directive({
  selector: '[appRevealPassword]',
  standalone: true
})
export class RevealPasswordDirective implements AfterViewInit {
  private _button: HTMLButtonElement;
  private _icon: HTMLElement;

  constructor(private _input: ElementRef<HTMLInputElement>) {}

  ngAfterViewInit(): void {
    this._button = this._input.nativeElement.parentElement.parentElement.querySelector('button') as HTMLButtonElement;
    if (this._button) {
      this._button.addEventListener('click', this.onClick.bind(this));
      this._icon = this._button.parentElement.querySelector('mat-icon');
    }
  }

  onClick(event: MouseEvent) {
    event.preventDefault();
    const isPassword = this._input.nativeElement.type === 'password';
    this._input.nativeElement.type = isPassword ? 'text' : 'password';
    this._icon.textContent = isPassword ? 'visibility_off' : 'visibility';
  }
}
