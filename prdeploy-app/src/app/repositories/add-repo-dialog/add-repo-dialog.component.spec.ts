import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { of, Subject } from 'rxjs';
import { OwnerRepoAddEnabledGQL } from 'src/app/shared/graphql';
import { NotificationManager } from 'src/app/shared/managers';
import { LoggingService } from 'src/app/shared/services';
import { AddRepoDialogComponent } from './add-repo-dialog.component';

/** Verifies the add repository dialog keeps the user in place and explains why an add was rejected. */
describe('AddRepoDialogComponent', () => {
  let addEnabledGql: jasmine.SpyObj<OwnerRepoAddEnabledGQL>;
  let notificationManager: jasmine.SpyObj<NotificationManager>;
  let dialogRef: jasmine.SpyObj<MatDialogRef<AddRepoDialogComponent>>;
  let component: AddRepoDialogComponent;

  beforeEach(() => {
    addEnabledGql = jasmine.createSpyObj<OwnerRepoAddEnabledGQL>('OwnerRepoAddEnabledGQL', ['mutate']);
    notificationManager = jasmine.createSpyObj<NotificationManager>('NotificationManager', ['show']);
    dialogRef = jasmine.createSpyObj<MatDialogRef<AddRepoDialogComponent>>('MatDialogRef', ['close', 'afterOpened']);
    dialogRef.afterOpened.and.returnValue(new Subject<void>());

    TestBed.configureTestingModule({
      imports: [AddRepoDialogComponent],
      providers: [
        { provide: OwnerRepoAddEnabledGQL, useValue: addEnabledGql },
        { provide: NotificationManager, useValue: notificationManager },
        { provide: LoggingService, useValue: jasmine.createSpyObj<LoggingService>('LoggingService', ['error']) },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { owner: 'journosa' } }
      ]
    });

    component = TestBed.createComponent(AddRepoDialogComponent).componentInstance;
    component.form.setValue({ owner: 'journosa', repo: 'journosa' });
  });

  /** Stubs the add mutation to resolve with the given status response. */
  function respondWith(success: boolean, message: string | null = null): void {
    addEnabledGql.mutate.and.returnValue(
      of({ data: { ownerRepoAddEnabled: { __typename: 'StatusResponse', success, message } } }) as never
    );
  }

  it('closes with the repository when the add succeeds', async () => {
    respondWith(true);

    await component.add();

    expect(dialogRef.close).toHaveBeenCalledWith({ owner: 'journosa', repo: 'journosa' });
    expect(component.errorMessage).toBeNull();
  });

  it('shows the API reason and stays open when the user is not a contributor', async () => {
    respondWith(false, 'You must be a contributor on journosa/journosa to add it.');

    await component.add();

    expect(dialogRef.close).not.toHaveBeenCalled();
    expect(component.errorMessage).toBe('You must be a contributor on journosa/journosa to add it.');
    expect(notificationManager.show).toHaveBeenCalledWith(
      'You must be a contributor on journosa/journosa to add it.',
      'danger'
    );
    expect(component.processing).toBeFalse();
  });

  it('falls back to a generic message when a rejection has no reason', async () => {
    respondWith(false);

    await component.add();

    expect(component.errorMessage).toBe('Unable to add repository journosa/journosa.');
    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('clears the error once the user edits the form', async () => {
    respondWith(false, 'Rejected.');
    await component.add();

    component.form.patchValue({ repo: 'other' });

    expect(component.errorMessage).toBeNull();
  });
});
