import { TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { DeploySettingsCompare, RepositoryWorkflowsGQL } from 'src/app/shared/graphql';
import { RepoManager } from 'src/app/shared/managers';
import { LoggingService } from 'src/app/shared/services';
import { DeployFormComponent } from './deploy-form.component';

/** Verifies the deploy form loads repository workflows for its GitHub workflow dropdown. */
describe('DeployFormComponent', () => {
  let workflowsGql: jasmine.SpyObj<RepositoryWorkflowsGQL>;
  let loggingService: jasmine.SpyObj<LoggingService>;

  /** Creates the component with mocked dependencies. */
  function create(): DeployFormComponent {
    TestBed.configureTestingModule({
      imports: [DeployFormComponent, NoopAnimationsModule],
      providers: [
        { provide: RepositoryWorkflowsGQL, useValue: workflowsGql },
        { provide: RepoManager, useValue: { owner: 'o', repo: 'r' } },
        { provide: LoggingService, useValue: loggingService }
      ]
    });
    return TestBed.createComponent(DeployFormComponent).componentInstance;
  }

  /** Builds a settings compare object with portal URL values per level. */
  function portalCompare(urls: Record<string, string | null>): DeploySettingsCompare {
    return { prdeployPortalUrl: urls, deployWorkflow: {}, environments: {} } as unknown as DeploySettingsCompare;
  }

  /** Builds a settings compare object with a deploy workflow at the repo level. */
  function compare(workflow: string | null): DeploySettingsCompare {
    return { deployWorkflow: { repo: workflow }, environments: {} } as unknown as DeploySettingsCompare;
  }

  beforeEach(() => {
    workflowsGql = jasmine.createSpyObj<RepositoryWorkflowsGQL>('RepositoryWorkflowsGQL', ['fetch']);
    loggingService = jasmine.createSpyObj<LoggingService>('LoggingService', ['error']);
  });

  it('loads workflow file names without folders', async () => {
    workflowsGql.fetch.and.returnValue(
      of({ data: { repositoryWorkflows: ['.github/workflows/deploy.yaml', '.github/workflows/build.yml'] } }) as never
    );
    const component = create();

    await component.ngOnInit();

    expect(workflowsGql.fetch).toHaveBeenCalledWith({ variables: { input: { owner: 'o', repo: 'r' } } });
    expect(component.workflows).toEqual(['deploy.yaml', 'build.yml']);
    expect(component.loadingWorkflows).toBeFalse();
  });

  it('keeps the list empty and logs when loading fails', async () => {
    const error = new Error('boom');
    workflowsGql.fetch.and.returnValue(throwError(() => error));
    const component = create();

    await component.ngOnInit();

    expect(component.workflows).toEqual([]);
    expect(loggingService.error).toHaveBeenCalledWith(error);
    expect(component.loadingWorkflows).toBeFalse();
  });

  it('includes a configured workflow that is not in the repository', () => {
    const component = create();
    component.workflows = ['deploy.yaml'];
    component.level = 'repo';
    component.settingsCompare = compare('legacy.yaml');

    expect(component.workflowOptions).toEqual(['legacy.yaml', 'deploy.yaml']);
  });

  it('does not duplicate a configured workflow that exists', () => {
    const component = create();
    component.workflows = ['deploy.yaml'];
    component.level = 'repo';
    component.settingsCompare = compare('deploy.yaml');

    expect(component.workflowOptions).toEqual(['deploy.yaml']);
  });

  it('defaults an empty portal URL to the current base URL', () => {
    const component = create();
    const settings = portalCompare({ owner: null, repo: null });
    component.level = 'owner';
    component.settingsCompare = settings;

    expect(settings.prdeployPortalUrl['owner']).toBe(window.location.origin);
  });

  it('keeps an existing portal URL', () => {
    const component = create();
    const settings = portalCompare({ owner: 'https://portal.example.com' });
    component.level = 'owner';
    component.settingsCompare = settings;

    expect(settings.prdeployPortalUrl['owner']).toBe('https://portal.example.com');
  });

  it('does not default at repo level when the owner level provides a URL', () => {
    const component = create();
    const settings = portalCompare({ owner: 'https://portal.example.com', repo: null });
    component.level = 'repo';
    component.settingsCompare = settings;

    expect(settings.prdeployPortalUrl['repo']).toBeNull();
  });
});
