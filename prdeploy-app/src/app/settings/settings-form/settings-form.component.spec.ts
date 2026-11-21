import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { NEVER } from 'rxjs';
import { DeploySettingsCompareGQL, DeploySettingsSetGQL } from 'src/app/shared/graphql';
import { RepoManager } from 'src/app/shared/managers';
import { NotificationManager } from 'src/app/shared/managers';
import { LoggingService } from 'src/app/shared/services';
import { SettingsFormComponent } from './settings-form.component';

/** Verifies the settings navigation tabs of the settings form. */
describe('SettingsFormComponent', () => {
  /** Creates the component with its dependencies stubbed so no data is fetched. */
  function create(): SettingsFormComponent {
    TestBed.configureTestingModule({
      providers: [
        { provide: DeploySettingsCompareGQL, useValue: {} },
        { provide: DeploySettingsSetGQL, useValue: {} },
        { provide: RepoManager, useValue: { valueChanged$: NEVER } },
        { provide: NotificationManager, useValue: {} },
        { provide: MatDialog, useValue: {} },
        { provide: LoggingService, useValue: {} }
      ]
    });
    return TestBed.runInInjectionContext(
      () =>
        new SettingsFormComponent(
          {} as DeploySettingsCompareGQL,
          {} as DeploySettingsSetGQL,
          { valueChanged$: NEVER } as unknown as RepoManager,
          {} as NotificationManager,
          {} as MatDialog,
          {} as LoggingService
        )
    );
  }

  it('orders the tabs with Deployment directly after Services', () => {
    const ids = create().settingsTabs.map(t => t.id);

    expect(ids).toEqual(['environments', 'services', 'deployment', 'slack', 'teams', 'jira']);
  });

  it('selects the section from the route', () => {
    const component = create();

    component.section = 'slack';

    expect(component.activeTabId).toBe('slack');
  });

  it('falls back to Environments for an unknown or missing section', () => {
    const component = create();
    component.section = 'slack';

    component.section = 'bogus';
    expect(component.activeTabId).toBe('environments');

    component.section = 'jira';
    component.section = undefined;
    expect(component.activeTabId).toBe('environments');
  });

  it('emits the section id when a nav option is chosen', () => {
    const component = create();
    const emitted: string[] = [];
    component.sectionChange.subscribe(id => emitted.push(id));

    component.settingsNavChange({ options: [{ value: 'teams' }] } as never);

    expect(component.activeTabId).toBe('teams');
    expect(emitted).toEqual(['teams']);
  });

  it('saves Teams webhook URLs and the notifications flag at the chosen level', () => {
    const component = create() as unknown as {
      setTeams(input: object, compare: object): void;
    };
    const compare = {
      notificationsEnabled: { owner: true, repo: null as boolean | string | null },
      webhooks: {
        deployUrl: { owner: 'https://owner/deploy', repo: null as string | null },
        releaseUrl: { owner: null as string | null, repo: null as string | null }
      }
    };
    const input: { webhooks: object } = { webhooks: {} };
    const level = { compare, level: 'owner', hasValues: false };

    component.setTeams(input, level);

    expect(input).toEqual({
      webhooks: { deployUrl: 'https://owner/deploy' },
      notificationsEnabled: true
    } as never);
    expect(level.hasValues).toBeTrue();
  });

  it('keeps the Teams section when only a webhook URL has a value', () => {
    const component = create() as unknown as {
      setTeams(input: object, compare: object): void;
    };
    const compare = {
      notificationsEnabled: { owner: null as string | null, repo: null as string | null },
      webhooks: {
        deployUrl: { owner: null as string | null, repo: null as string | null },
        releaseUrl: { owner: null as string | null, repo: 'https://repo/release' }
      }
    };
    const level = { compare, level: 'repo', hasValues: false };

    component.setTeams({ webhooks: {} }, level);

    expect(level.hasValues).toBeTrue();
  });

  it('leaves the Teams section empty when nothing is set', () => {
    const component = create() as unknown as {
      setTeams(input: object, compare: object): void;
    };
    const compare = {
      notificationsEnabled: { owner: null as string | null, repo: null as string | null },
      webhooks: {
        deployUrl: { owner: null as string | null, repo: null as string | null },
        releaseUrl: { owner: null as string | null, repo: null as string | null }
      }
    };
    const level = { compare, level: 'repo', hasValues: false };

    component.setTeams({ webhooks: {} }, level);

    expect(level.hasValues).toBeFalse();
  });

  it('saves environment URLs without extra fields and drops incomplete rows', () => {
    const component = create() as unknown as { mapEnvironment(compare: object): { urls?: object[]; url?: string } };

    const input = component.mapEnvironment({
      name: 'Dev',
      url: null,
      urls: [
        { __typename: 'EnvironmentUrl', label: ' Web ', url: ' https://web.test ' },
        { label: 'No url', url: '' },
        { label: '', url: 'https://nolabel.test' }
      ]
    });

    expect(input.urls).toEqual([{ label: 'Web', url: 'https://web.test' }]);
    expect(input.url).toBeUndefined();
  });

  it('omits URLs from the saved environment when none are set', () => {
    const component = create() as unknown as { mapEnvironment(compare: object): { urls?: object[] } };

    expect(component.mapEnvironment({ name: 'Dev', urls: [] }).urls).toBeUndefined();
    expect(component.mapEnvironment({ name: 'Dev' }).urls).toBeUndefined();
  });
});
