import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { DeployEnvironmentRollbackGQL } from 'src/app/shared/graphql';
import { NotificationManager } from 'src/app/shared/managers';
import { LoggingService } from 'src/app/shared/services';
import { DeployRollbackDialogComponent } from './deploy-rollback-dialog.component';

/** Verifies the rollback dialog only submits a whole rollback count of at least one. */
describe('DeployRollbackDialogComponent', () => {
  /** Renders the dialog with mocked dependencies and returns its fixture. */
  function create() {
    TestBed.configureTestingModule({
      imports: [DeployRollbackDialogComponent, NoopAnimationsModule],
      providers: [
        {
          provide: DeployEnvironmentRollbackGQL,
          useValue: jasmine.createSpyObj('DeployEnvironmentRollbackGQL', ['mutate'])
        },
        { provide: NotificationManager, useValue: jasmine.createSpyObj('NotificationManager', ['show']) },
        { provide: LoggingService, useValue: jasmine.createSpyObj('LoggingService', ['error']) },
        { provide: MatDialogRef, useValue: { afterOpened: () => of(undefined), close: jasmine.createSpy('close') } },
        { provide: MAT_DIALOG_DATA, useValue: { environment: 'prod', pullNumber: 8 } }
      ]
    });
    const fixture = TestBed.createComponent(DeployRollbackDialogComponent);
    fixture.detectChanges();
    return fixture;
  }

  /** Finds the Rollback submit button. */
  function rollbackButton(element: HTMLElement): HTMLButtonElement {
    return Array.from(element.querySelectorAll('button')).find(b => b.textContent?.includes('Rollback'))!;
  }

  it('marks the rollback count as required', () => {
    const fixture = create();

    const input: HTMLInputElement = fixture.nativeElement.querySelector('input[formControlName="rollbackCount"]');
    expect(input.required).toBeTrue();
  });

  for (const value of [null, 0, -1, 1.5]) {
    it(`disables rollback for a count of ${value}`, () => {
      const fixture = create();

      fixture.componentInstance.form.controls.rollbackCount.setValue(value);
      fixture.detectChanges();

      expect(fixture.componentInstance.form.valid).toBeFalse();
      expect(rollbackButton(fixture.nativeElement).disabled).toBeTrue();
    });
  }

  it('enables rollback for a whole count of at least one', () => {
    const fixture = create();

    fixture.componentInstance.form.controls.rollbackCount.setValue(2);
    fixture.detectChanges();

    expect(fixture.componentInstance.form.valid).toBeTrue();
    expect(rollbackButton(fixture.nativeElement).disabled).toBeFalse();
  });
});
