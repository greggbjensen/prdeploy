import { TestBed } from '@angular/core/testing';
import { MatDialogRef } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { AddEnvironmentUrlDialogComponent } from './add-environment-url-dialog.component';

/** Verifies a label and an absolute http(s) URL are required before an environment URL can be added. */
describe('AddEnvironmentUrlDialogComponent', () => {
  /** Renders the dialog and returns its fixture and the close spy. */
  function create() {
    const close = jasmine.createSpy('close');
    TestBed.configureTestingModule({
      imports: [AddEnvironmentUrlDialogComponent, NoopAnimationsModule],
      providers: [{ provide: MatDialogRef, useValue: { afterOpened: () => of(undefined), close } }]
    });
    const fixture = TestBed.createComponent(AddEnvironmentUrlDialogComponent);
    fixture.detectChanges();
    return { fixture, close };
  }

  /** Finds the Add URL submit button. */
  function addButton(element: HTMLElement): HTMLButtonElement {
    return Array.from(element.querySelectorAll('button')).find(b => b.textContent?.includes('Add URL'))!;
  }

  it('disables adding until a label and URL are entered', () => {
    const { fixture } = create();
    expect(addButton(fixture.nativeElement).disabled).toBeTrue();

    fixture.componentInstance.form.patchValue({ label: 'Web' });
    fixture.detectChanges();
    expect(addButton(fixture.nativeElement).disabled).toBeTrue();

    fixture.componentInstance.form.patchValue({ url: 'https://web.test' });
    fixture.detectChanges();
    expect(addButton(fixture.nativeElement).disabled).toBeFalse();
  });

  it('rejects a URL that is not an absolute http or https address', () => {
    const { fixture } = create();

    for (const url of ['web.test', 'javascript:alert(1)', 'ftp://web.test', 'https://']) {
      fixture.componentInstance.form.patchValue({ label: 'Web', url });
      expect(fixture.componentInstance.form.valid).withContext(url).toBeFalse();
    }
  });

  it('closes with the trimmed label and URL', () => {
    const { fixture, close } = create();
    fixture.componentInstance.form.patchValue({ label: ' Web ', url: 'https://web.test' });

    fixture.componentInstance.add();

    expect(close).toHaveBeenCalledWith({ label: 'Web', url: 'https://web.test' });
  });

  it('closes with null when cancelled', () => {
    const { fixture, close } = create();

    fixture.componentInstance.cancel();

    expect(close).toHaveBeenCalledWith(null);
  });
});
