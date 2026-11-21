import { TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { TeamsSettingsCompare } from 'src/app/shared/graphql';
import { TeamsFormComponent } from './teams-form.component';

/** Verifies the Microsoft Teams form shows only the notifications flag and the two webhook URLs. */
describe('TeamsFormComponent', () => {
  /** Builds compare data with an owner deploy URL set. */
  function teams(): TeamsSettingsCompare {
    return {
      notificationsEnabled: { owner: true, repo: null },
      webhooks: {
        deployUrl: { owner: 'https://owner/deploy', repo: null },
        releaseUrl: { owner: null, repo: null }
      }
    } as TeamsSettingsCompare;
  }

  /** Creates the form for a settings level. */
  function create(level: 'owner' | 'repo') {
    TestBed.configureTestingModule({ imports: [TeamsFormComponent, NoopAnimationsModule] });
    const fixture = TestBed.createComponent(TeamsFormComponent);
    fixture.componentInstance.teams = teams();
    fixture.componentInstance.level = level;
    fixture.detectChanges();
    return fixture;
  }

  it('shows the notifications checkbox and only the deploy and release URL fields', async () => {
    const fixture = create('owner');
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.textContent).toContain('Notifications enabled');
    expect(element.querySelectorAll('input[matInput]').length).toBe(2);
    expect(element.textContent).toContain('Deploy URL');
    expect(element.textContent).toContain('Release URL');
  });

  it('shows owner values as placeholders at repo level', () => {
    expect(create('repo').componentInstance.showOwner).toBeTrue();
  });

  // TestBed can only be configured once per spec, so each level is created in its own spec.
  it('does not show owner placeholders at owner level', () => {
    expect(create('owner').componentInstance.showOwner).toBeFalse();
  });
});
