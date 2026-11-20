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

  it('saves Teams webhook URLs and the notifications flag at the chosen level', () => {
    const component = create() as unknown as {
      setTeams(input: object, compare: object): void;
    };
    const compare = {
      notificationsEnabled: { owner: true, repo: null },
      webhooks: { deployUrl: { owner: 'https://owner/deploy', repo: null }, releaseUrl: { owner: null, repo: null } }
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
      notificationsEnabled: { owner: null, repo: null },
      webhooks: { deployUrl: { owner: null, repo: null }, releaseUrl: { owner: null, repo: 'https://repo/release' } }
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
      notificationsEnabled: { owner: null, repo: null },
      webhooks: { deployUrl: { owner: null, repo: null }, releaseUrl: { owner: null, repo: null } }
    };
    const level = { compare, level: 'repo', hasValues: false };

    component.setTeams({ webhooks: {} }, level);

    expect(level.hasValues).toBeFalse();
  });
});
