import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { BehaviorSubject, NEVER } from 'rxjs';
import { RepoManager, RouteManager } from '../shared/managers';
import { SettingsComponent } from './settings.component';

/** Verifies the deep routing of the settings page and its secondary left nav. */
describe('SettingsComponent', () => {
  let params$: BehaviorSubject<Record<string, string>>;
  let navigate: jasmine.Spy;

  /** Creates the component with the route and managers stubbed. */
  function create(): SettingsComponent {
    params$ = new BehaviorSubject<Record<string, string>>({ level: 'owner' });
    navigate = jasmine.createSpy('navigate');
    TestBed.configureTestingModule({});
    return TestBed.runInInjectionContext(
      () =>
        new SettingsComponent(
          { owner: 'o', repo: 'r', valueChanged$: NEVER } as unknown as RepoManager,
          { navigate } as unknown as RouteManager,
          { params: params$ } as unknown as ActivatedRoute
        )
    );
  }

  it('reads the level and section from the route', () => {
    const component = create();

    params$.next({ level: 'repo', section: 'slack' });

    expect(component.level).toBe('repo');
    expect(component.selectedTabIndex).toBe(1);
    expect(component.section).toBe('slack');
  });

  it('has no section when the route omits it', () => {
    const component = create();

    expect(component.section).toBeUndefined();
  });

  it('navigates to the section route when a section is chosen', () => {
    const component = create();

    component.selectedSectionChanged('jira');

    expect(navigate).toHaveBeenCalledWith(['/', 'o', 'r', 'settings', 'owner', 'jira']);
  });

  it('keeps the section when the level tab changes', () => {
    const component = create();
    params$.next({ level: 'owner', section: 'teams' });

    component.selectedLevelChanged({ index: 1 } as never);

    expect(navigate).toHaveBeenCalledWith(['/', 'o', 'r', 'settings', 'repo', 'teams']);
  });

  it('does not navigate again for the section already shown', () => {
    const component = create();
    params$.next({ level: 'owner', section: 'jira' });

    component.selectedSectionChanged('jira');

    expect(navigate).not.toHaveBeenCalled();
  });
});
