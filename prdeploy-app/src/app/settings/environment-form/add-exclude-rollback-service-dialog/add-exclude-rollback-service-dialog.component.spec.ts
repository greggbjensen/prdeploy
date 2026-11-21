import { TestBed } from '@angular/core/testing';
import { MatDialogRef } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { AddExcludeRollbackServiceDialogComponent } from './add-exclude-rollback-service-dialog.component';

/** Verifies a service name is required before it can be excluded from rollback. */
describe('AddExcludeRollbackServiceDialogComponent', () => {
  /** Renders the dialog and returns its fixture. */
  function create() {
    TestBed.configureTestingModule({
      imports: [AddExcludeRollbackServiceDialogComponent, NoopAnimationsModule],
      providers: [
        { provide: MatDialogRef, useValue: { afterOpened: () => of(undefined), close: jasmine.createSpy('close') } }
      ]
    });
    const fixture = TestBed.createComponent(AddExcludeRollbackServiceDialogComponent);
    fixture.detectChanges();
    return fixture;
  }

  /** Finds the Add Exclude submit button. */
  function addButton(element: HTMLElement): HTMLButtonElement {
    return Array.from(element.querySelectorAll('button')).find(b => b.textContent?.includes('Add Exclude'))!;
  }

  it('disables adding until a name is entered', () => {
    const fixture = create();

    expect(addButton(fixture.nativeElement).disabled).toBeTrue();

    fixture.componentInstance.form.controls.name.setValue('api');
    fixture.detectChanges();

    expect(addButton(fixture.nativeElement).disabled).toBeFalse();
  });
});
