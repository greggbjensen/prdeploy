import { TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { PullRequestAddServicesGQL, PullRequestServicesGQL } from 'src/app/shared/graphql';
import { NotificationManager, RepoManager } from 'src/app/shared/managers';
import { DialogResult } from 'src/app/shared/models';
import { LoggingService } from 'src/app/shared/services';
import { PrServicesDialogComponent } from './pr-services-dialog.component';

/** Verifies the pull request services dialog shows triggered services locked and adds only the newly checked ones. */
describe('PrServicesDialogComponent', () => {
  const services = [
    { name: 'api', triggered: true },
    { name: 'app', triggered: false },
    { name: 'docs', triggered: false }
  ];

  let fetch: jasmine.Spy;
  let mutate: jasmine.Spy;
  let close: jasmine.Spy;
  let show: jasmine.Spy;
  let error: jasmine.Spy;

  /** Creates the dialog with stubbed GraphQL services, then waits for the services to load. */
  async function create(loaded = services) {
    fetch = jasmine.createSpy('fetch').and.returnValue(of({ data: { pullRequestServices: loaded } }));
    mutate = jasmine.createSpy('mutate').and.returnValue(of({}));
    close = jasmine.createSpy('close');
    show = jasmine.createSpy('show');
    error = jasmine.createSpy('error');
    TestBed.configureTestingModule({
      imports: [PrServicesDialogComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: RepoManager, useValue: { owner: 'o', repo: 'r' } },
        { provide: MAT_DIALOG_DATA, useValue: { pullNumber: 7 } },
        { provide: MatDialogRef, useValue: { close } },
        { provide: PullRequestServicesGQL, useValue: { fetch } },
        { provide: PullRequestAddServicesGQL, useValue: { mutate } },
        { provide: NotificationManager, useValue: { show } },
        { provide: LoggingService, useValue: { error } }
      ]
    });
    const fixture = TestBed.createComponent(PrServicesDialogComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  /** Returns the rendered service options. */
  function options(root: HTMLElement): HTMLElement[] {
    return Array.from(root.querySelectorAll<HTMLElement>('mat-list-option'));
  }

  it('loads the services for the pull request', async () => {
    await create();

    expect(fetch.calls.mostRecent().args[0].variables.input).toEqual({ owner: 'o', repo: 'r', pullNumber: 7 });
  });

  it('shows triggered services checked and disabled, and others unchecked', async () => {
    const fixture = await create();

    const [api, app] = options(fixture.nativeElement);
    expect(api.getAttribute('aria-selected')).toBe('true');
    expect(api.getAttribute('aria-disabled')).toBe('true');
    expect(app.getAttribute('aria-selected')).toBe('false');
    expect(app.getAttribute('aria-disabled')).not.toBe('true');
  });

  it('disables the add button until an extra service is checked', async () => {
    const fixture = await create();

    const button = fixture.nativeElement.querySelector('mat-dialog-actions .mat-mdc-unelevated-button') as HTMLButtonElement;
    expect(button.disabled).toBeTrue();
  });

  it('adds only the newly checked services and closes', async () => {
    const fixture = await create();
    options(fixture.nativeElement)[2].click();
    fixture.detectChanges();

    await fixture.componentInstance.addServices();

    expect(fixture.componentInstance.selectedServices).toEqual(['docs']);
    expect(mutate.calls.mostRecent().args[0].variables.input).toEqual({
      owner: 'o',
      repo: 'r',
      pullNumber: 7,
      services: ['docs']
    });
    expect(show).toHaveBeenCalled();
    expect(close).toHaveBeenCalledWith(DialogResult.Save);
  });

  it('keeps the dialog open and logs when adding fails', async () => {
    const fixture = await create();
    fixture.componentInstance.selectedServices = ['app'];
    mutate.and.returnValue(throwError(() => new Error('boom')));

    await fixture.componentInstance.addServices();

    expect(error).toHaveBeenCalled();
    expect(close).not.toHaveBeenCalled();
    expect(fixture.componentInstance.processing).toBeFalse();
  });

  it('points to settings when no services are configured', async () => {
    const fixture = await create([]);

    expect(options(fixture.nativeElement).length).toBe(0);
    expect(fixture.nativeElement.querySelector('app-alert-panel')).not.toBeNull();
  });

  it('closes with cancel', async () => {
    const fixture = await create();

    fixture.componentInstance.cancel();

    expect(close).toHaveBeenCalledWith(DialogResult.Cancel);
  });
});
