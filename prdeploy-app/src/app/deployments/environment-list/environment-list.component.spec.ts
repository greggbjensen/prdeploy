import { TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { MtxPopoverTrigger } from '@ng-matero/extensions/popover';
import { DeployEnvironment, DeployEnvironmentDeployGQL, DeployEnvironmentFreeGQL } from 'src/app/shared/graphql';
import { NotificationManager, RepoManager } from 'src/app/shared/managers';
import { LoggingService } from 'src/app/shared/services';
import { EnvironmentListComponent } from './environment-list.component';

/** Verifies the pull request hover preview and the labelled URL links in the deployments table. */
describe('EnvironmentListComponent', () => {
  /** Configures the testing module with stubbed dependencies. */
  function configure(): void {
    TestBed.configureTestingModule({
      imports: [EnvironmentListComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: RepoManager, useValue: {} },
        { provide: DeployEnvironmentDeployGQL, useValue: {} },
        { provide: DeployEnvironmentFreeGQL, useValue: {} },
        { provide: NotificationManager, useValue: {} },
        { provide: LoggingService, useValue: {} },
        { provide: MatDialog, useValue: {} }
      ]
    });
  }

  /** Renders the list with one environment and opens its pull request popover. */
  function openPreview(): HTMLElement {
    configure();
    const fixture = TestBed.createComponent(EnvironmentListComponent);
    fixture.componentInstance.data = [
      {
        name: 'Dev',
        pullRequest: {
          number: 42,
          title: 'Add login page',
          url: 'https://example.test/pull/42',
          body: 'Body text',
          updatedAt: '2026-01-01T00:00:00Z',
          user: { name: 'Ada' }
        }
      } as unknown as DeployEnvironment
    ];
    fixture.detectChanges();
    const trigger = fixture.debugElement.query(el => el.injector.get(MtxPopoverTrigger, null) !== null);
    trigger.injector.get(MtxPopoverTrigger).openPopover();
    fixture.detectChanges();
    return document.body;
  }

  it('shows the pull request number link on the same row as the title, above the details', () => {
    const body = openPreview();

    const title = body.querySelector('.pull-title')!;
    const link = title.querySelector<HTMLAnchorElement>('.pull-link')!;
    expect(link.textContent?.trim()).toBe('#42');
    expect(link.getAttribute('href')).toBe('https://example.test/pull/42');
    expect(link.getAttribute('target')).toBe('_blank');
    expect(title.textContent).toContain('Add login page');

    expect(title.nextElementSibling?.classList).toContain('pull-head');
    expect(body.querySelector('.pull-head')?.nextElementSibling?.tagName.toLowerCase()).toBe('markdown');
  });

  /** Renders the list with the supplied environments and returns the host element. */
  function renderUrls(urls: { label: string; url: string }[] | undefined): HTMLElement {
    configure();
    const fixture = TestBed.createComponent(EnvironmentListComponent);
    fixture.componentInstance.data = [{ name: 'Dev', urls } as unknown as DeployEnvironment];
    fixture.detectChanges();
    return fixture.nativeElement;
  }

  it('shows each environment URL as a label followed by a link', () => {
    const host = renderUrls([
      { label: 'Web', url: 'https://web.test' },
      { label: 'Admin', url: 'https://admin.test/login' }
    ]);

    const rows = Array.from(host.querySelectorAll('.environment-url'));
    expect(rows.map(r => r.querySelector('.environment-url-label')?.textContent?.trim())).toEqual(['Web', 'Admin']);
    const links = rows.map(r => r.querySelector('a')!);
    expect(links.map(a => a.getAttribute('href'))).toEqual(['https://web.test', 'https://admin.test/login']);
    expect(links.every(a => a.getAttribute('target') === '_blank')).toBeTrue();
    expect(links[1].textContent?.trim()).toBe('https://admin.test/login');
  });

  it('titles the column URLs and does not bold the labels', () => {
    const host = renderUrls([{ label: 'Web', url: 'https://web.test' }]);

    expect(host.querySelector('th.mat-column-url')?.textContent?.trim()).toBe('URLs');
    const label = host.querySelector('.environment-url-label')!;
    expect(Number(getComputedStyle(label).fontWeight)).toBeLessThan(600);
  });

  it('aligns the URL links horizontally when the labels differ in length', () => {
    const host = renderUrls([
      { label: 'A', url: 'https://a.test' },
      { label: 'Administration', url: 'https://admin.test' }
    ]);

    const lefts = Array.from(host.querySelectorAll<HTMLAnchorElement>('.environment-url a')).map(
      a => a.getBoundingClientRect().left
    );
    expect(lefts.length).toBe(2);
    expect(lefts[0]).toBeCloseTo(lefts[1], 0);
    const labelRights = Array.from(host.querySelectorAll('.environment-url-label')).map(
      l => l.getBoundingClientRect().right
    );
    expect(lefts[0]).toBeGreaterThan(Math.max(...labelRights));
  });

  /** Renders the list with one environment whose pull request was opened by the supplied user. */
  function renderUser(user: object): HTMLElement {
    configure();
    const fixture = TestBed.createComponent(EnvironmentListComponent);
    fixture.componentInstance.data = [
      {
        name: 'Dev',
        pullRequest: { number: 1, title: 'T', url: 'https://example.test/pull/1', user }
      } as unknown as DeployEnvironment
    ];
    fixture.detectChanges();
    return fixture.nativeElement;
  }

  it('shows the user avatar and name as a link to their GitHub profile', () => {
    const host = renderUser({
      name: 'Ada Lovelace',
      username: 'ada',
      avatarUrl: 'https://avatars.githubusercontent.com/u/1?v=4',
      url: 'https://github.com/ada'
    });

    const link = host.querySelector<HTMLAnchorElement>('td.mat-column-user a.environment-user')!;
    expect(link.getAttribute('href')).toBe('https://github.com/ada');
    expect(link.getAttribute('target')).toBe('_blank');
    expect(link.textContent?.trim()).toBe('Ada Lovelace');
    const avatar = link.querySelector<HTMLImageElement>('img.environment-user-avatar')!;
    expect(avatar.getAttribute('src')).toBe('https://avatars.githubusercontent.com/u/1?v=4');
    expect(avatar.getAttribute('alt')).toBe('');
  });

  it('shows the name without an avatar or link when GitHub returned neither', () => {
    const host = renderUser({ name: 'Ada Lovelace', username: 'ada' });

    const link = host.querySelector('td.mat-column-user a.environment-user')!;
    expect(link.hasAttribute('href')).toBeFalse();
    expect(link.querySelector('img')).toBeNull();
    expect(link.textContent?.trim()).toBe('Ada Lovelace');
  });

  it('shows no URL rows when the environment has none', () => {
    expect(renderUrls([]).querySelectorAll('.environment-url').length).toBe(0);
    expect(renderUrls(undefined).querySelectorAll('.environment-url').length).toBe(0);
  });

  it('opens the services dialog for the pull request from the Services action', () => {
    configure();
    const open = jasmine.createSpy('open');
    TestBed.overrideProvider(MatDialog, { useValue: { open } });
    const fixture = TestBed.createComponent(EnvironmentListComponent);
    fixture.componentInstance.data = [
      { name: 'Dev', pullRequest: { number: 42, title: 'Add login page' } } as unknown as DeployEnvironment
    ];
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[title="Services"]') as HTMLButtonElement;
    button.click();

    expect(open).toHaveBeenCalledTimes(1);
    expect(open.calls.mostRecent().args[1].data).toEqual({ pullNumber: 42 });
  });

  it('hides the Services action when an environment has no pull request', () => {
    configure();
    const fixture = TestBed.createComponent(EnvironmentListComponent);
    fixture.componentInstance.data = [{ name: 'Dev' } as unknown as DeployEnvironment];
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('button[title="Services"]')).toBeNull();
  });
});
