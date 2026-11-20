import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, Subject, throwError } from 'rxjs';
import { RepositoryWorkflowsGQL } from 'src/app/shared/graphql';
import { RepoManager } from 'src/app/shared/managers';
import { LoggingService } from 'src/app/shared/services';
import { AddServiceDialogComponent } from './add-service-dialog.component';

/** Verifies the add service dialog lists workflows and derives the service name from the selected one. */
describe('AddServiceDialogComponent', () => {
  let workflowsGql: jasmine.SpyObj<RepositoryWorkflowsGQL>;
  let loggingService: jasmine.SpyObj<LoggingService>;
  let dialogRef: jasmine.SpyObj<MatDialogRef<AddServiceDialogComponent>>;

  /** Creates the component with the supplied dialog data. */
  function create(data: unknown = { workflowPattern: '/([^/]+?)-build.ya?ml' }): AddServiceDialogComponent {
    TestBed.configureTestingModule({
      imports: [AddServiceDialogComponent, NoopAnimationsModule],
      providers: [
        { provide: RepositoryWorkflowsGQL, useValue: workflowsGql },
        { provide: RepoManager, useValue: { owner: 'o', repo: 'r' } },
        { provide: LoggingService, useValue: loggingService },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: data }
      ]
    });
    return TestBed.createComponent(AddServiceDialogComponent).componentInstance;
  }

  beforeEach(() => {
    workflowsGql = jasmine.createSpyObj<RepositoryWorkflowsGQL>('RepositoryWorkflowsGQL', ['fetch']);
    loggingService = jasmine.createSpyObj<LoggingService>('LoggingService', ['error']);
    dialogRef = jasmine.createSpyObj<MatDialogRef<AddServiceDialogComponent>>('MatDialogRef', ['close', 'afterOpened']);
    dialogRef.afterOpened.and.returnValue(new Subject<void>());
  });

  it('loads the repository workflows', async () => {
    workflowsGql.fetch.and.returnValue(
      of({ data: { repositoryWorkflows: ['.github/workflows/api-build.yaml'] } }) as never
    );
    const component = create();

    await component.ngOnInit();

    expect(workflowsGql.fetch).toHaveBeenCalledWith({ variables: { input: { owner: 'o', repo: 'r' } } });
    expect(component.workflows).toEqual(['.github/workflows/api-build.yaml']);
    expect(component.loadingWorkflows).toBeFalse();
  });

  it('fills the service name when a workflow is selected', () => {
    const component = create();

    component.form.controls.workflow.setValue('.github/workflows/prdeploy-api-build.yaml');

    expect(component.form.controls.name.value).toBe('prdeploy-api');
  });

  it('falls back to the file name when the dialog has no pattern', () => {
    const component = create(null);

    component.form.controls.workflow.setValue('.github/workflows/web-build.yml');

    expect(component.form.controls.name.value).toBe('web-build');
  });

  it('keeps the name when the workflow selection is cleared', () => {
    const component = create();
    component.form.controls.workflow.setValue('.github/workflows/web-build.yml');

    component.form.controls.workflow.setValue(null);

    expect(component.form.controls.name.value).toBe('web');
  });

  it('logs the error and leaves the list empty when loading fails', async () => {
    const error = new Error('boom');
    workflowsGql.fetch.and.returnValue(throwError(() => error));
    const component = create();

    await component.ngOnInit();

    expect(loggingService.error).toHaveBeenCalledWith(error);
    expect(component.workflows).toEqual([]);
    expect(component.loadingWorkflows).toBeFalse();
  });

  it('closes with the typed service name and no workflow when none was selected', () => {
    const component = create();
    component.form.controls.name.setValue('custom');

    component.add();

    expect(dialogRef.close).toHaveBeenCalledWith({ name: 'custom', workflow: undefined });
  });

  it('closes with the selected workflow file name', () => {
    const component = create();
    component.form.controls.workflow.setValue('.github/workflows/prdeploy-api-build.yaml');

    component.add();

    expect(dialogRef.close).toHaveBeenCalledWith({ name: 'prdeploy-api', workflow: 'prdeploy-api-build.yaml' });
  });

  it('requires a service name', () => {
    const component = create();

    expect(component.form.valid).toBeFalse();
  });
});
