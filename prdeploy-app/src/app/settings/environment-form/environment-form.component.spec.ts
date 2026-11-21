import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { EnvironmentSettings } from 'src/app/shared/graphql';
import { EnvironmentFormComponent } from './environment-form.component';

/** Verifies the URLs table of the environment settings form. */
describe('EnvironmentFormComponent URLs', () => {
  /** Builds an environment with the supplied URL fields. */
  function environment(extra: Partial<EnvironmentSettings> = {}): EnvironmentSettings {
    return {
      name: 'Dev',
      queue: 'DEV_PR_QUEUE',
      color: '#fff',
      requireApproval: false,
      requireBranchUpToDate: false,
      automationTest: { enabled: false },
      ...extra
    } as EnvironmentSettings;
  }

  /** Renders the form for an environment with a dialog stub that resolves to the given result. */
  function create(env: EnvironmentSettings, dialogResult: unknown = null) {
    TestBed.configureTestingModule({
      imports: [EnvironmentFormComponent, NoopAnimationsModule],
      providers: [{ provide: MatDialog, useValue: { open: () => ({ afterClosed: () => of(dialogResult) }) } }]
    });
    const fixture = TestBed.createComponent(EnvironmentFormComponent);
    fixture.componentInstance.environment = env;
    fixture.componentInstance.hasEnvironments = true;
    fixture.detectChanges();
    return fixture;
  }

  /** Reads the label and URL values of each rendered URL row. */
  function rows(element: HTMLElement): string[][] {
    return Array.from(element.querySelectorAll('table.environment-urls tr.mat-mdc-row')).map(r =>
      Array.from(r.querySelectorAll('input')).map(i => i.value)
    );
  }

  it('lists the configured URLs as editable label and URL rows', async () => {
    const fixture = create(environment({ urls: [{ label: 'Web', url: 'https://web.test' }] }));
    await fixture.whenStable();

    expect(rows(fixture.nativeElement)).toEqual([['Web', 'https://web.test']]);
  });

  it('moves a legacy single URL into the list', () => {
    const env = environment({ url: ' https://legacy.test ' });

    create(env);

    expect(env.urls).toEqual([{ label: 'URL', url: 'https://legacy.test' }]);
    expect(env.url).toBeNull();
  });

  it('keeps existing URLs and drops the legacy URL', () => {
    const env = environment({ url: 'https://legacy.test', urls: [{ label: 'Web', url: 'https://web.test' }] });

    create(env);

    expect(env.urls).toEqual([{ label: 'Web', url: 'https://web.test' }]);
    expect(env.url).toBeNull();
  });

  it('adds the URL chosen in the dialog', async () => {
    const env = environment();
    const fixture = create(env, { label: 'Admin', url: 'https://admin.test' });

    await fixture.componentInstance.showAddUrl();

    expect(env.urls).toEqual([{ label: 'Admin', url: 'https://admin.test' }]);
  });

  it('renders a newly added URL as its own row and keeps the existing rows', async () => {
    const fixture = create(environment({ urls: [{ label: 'Web', url: 'https://web.test' }] }), {
      label: 'Admin',
      url: 'https://admin.test'
    });
    await fixture.whenStable();

    await fixture.componentInstance.showAddUrl();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(rows(fixture.nativeElement)).toEqual([
      ['Web', 'https://web.test'],
      ['Admin', 'https://admin.test']
    ]);
  });

  it('keeps each row bound to its own URL when one row is edited', async () => {
    const env = environment({
      urls: [
        { label: 'Web', url: 'https://web.test' },
        { label: 'Admin', url: 'https://admin.test' }
      ]
    });
    const fixture = create(env);
    await fixture.whenStable();

    const input: HTMLInputElement = fixture.nativeElement.querySelector('table.environment-urls tr.mat-mdc-row input');
    input.value = 'Site';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    await fixture.whenStable();

    expect(env.urls!.map(u => u.label)).toEqual(['Site', 'Admin']);
    expect(rows(fixture.nativeElement)).toEqual([
      ['Site', 'https://web.test'],
      ['Admin', 'https://admin.test']
    ]);
  });

  it('removes a URL row from the table', async () => {
    const env = environment({
      urls: [
        { label: 'Web', url: 'https://web.test' },
        { label: 'Admin', url: 'https://admin.test' }
      ]
    });
    const fixture = create(env);
    await fixture.whenStable();

    fixture.componentInstance.removeUrl(env.urls![0]);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(rows(fixture.nativeElement)).toEqual([['Admin', 'https://admin.test']]);
  });

  it('does not add a URL when the dialog is cancelled', async () => {
    const env = environment();
    const fixture = create(env, null);

    await fixture.componentInstance.showAddUrl();

    expect(env.urls).toBeUndefined();
  });

  it('removes a URL', () => {
    const first = { label: 'Web', url: 'https://web.test' };
    const second = { label: 'Admin', url: 'https://admin.test' };
    const env = environment({ urls: [first, second] });
    const fixture = create(env);

    fixture.componentInstance.removeUrl(first);

    expect(env.urls).toEqual([second]);
  });
});
