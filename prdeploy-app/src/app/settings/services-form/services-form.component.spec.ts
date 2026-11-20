import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { OwnerRepoValueOfListOfServiceSettings } from 'src/app/shared/graphql';
import { ServicesFormComponent } from './services-form.component';

/** Verifies the services form shows the workflow column first and records the workflow of added services. */
describe('ServicesFormComponent', () => {
  let dialog: jasmine.SpyObj<MatDialog>;
  let component: ServicesFormComponent;

  beforeEach(() => {
    dialog = jasmine.createSpyObj<MatDialog>('MatDialog', ['open']);
    TestBed.configureTestingModule({
      imports: [ServicesFormComponent, NoopAnimationsModule],
      providers: [{ provide: MatDialog, useValue: dialog }]
    });
    component = TestBed.createComponent(ServicesFormComponent).componentInstance;
    component.level = 'repo';
    component.services = { owner: [], repo: [] } as OwnerRepoValueOfListOfServiceSettings;
  });

  it('shows the workflow column before the name column', () => {
    expect(component.displayColumns).toEqual(['workflow', 'name', 'path', 'remove']);
  });

  it('adds a service with its workflow', () => {
    component.add('api', 'api-build.yaml');

    expect(component.actingServiceList).toEqual([{ name: 'api', path: 'api', workflow: 'api-build.yaml' }]);
  });

  it('adds a service without a workflow when typed by hand', () => {
    component.add('api');

    expect(component.actingServiceList).toEqual([{ name: 'api', path: 'api', workflow: undefined }]);
  });

  it('adds a service with an explicit path', () => {
    component.add('api', undefined, 'src/api');

    expect(component.actingServiceList[0].path).toBe('src/api');
  });

  it('does not add a duplicate service name', () => {
    component.add('api', 'api-build.yaml');
    component.add('api', 'other-build.yaml');

    expect(component.actingServiceList.length).toBe(1);
  });

  it('adds the service chosen in the dialog', async () => {
    dialog.open.and.returnValue({
      afterClosed: () => of({ name: 'web', path: 'web-path', workflow: 'web-build.yml' })
    } as never);

    await component.showAddDialog();

    expect(component.actingServiceList[0].workflow).toBe('web-build.yml');
    expect(component.actingServiceList[0].path).toBe('web-path');
  });

  it('ignores a cancelled dialog', async () => {
    dialog.open.and.returnValue({ afterClosed: () => of(null) } as never);

    await component.showAddDialog();

    expect(component.actingServiceList).toEqual([]);
  });

  it('uses the repo pattern at repo level and falls back to the owner pattern', () => {
    component.workflowPattern = { owner: 'owner-pattern', repo: null } as never;
    expect(component.effectiveWorkflowPattern()).toBe('owner-pattern');

    component.workflowPattern = { owner: 'owner-pattern', repo: 'repo-pattern' } as never;
    expect(component.effectiveWorkflowPattern()).toBe('repo-pattern');
  });

  it('uses only the owner pattern at owner level', () => {
    component.level = 'owner';
    component.workflowPattern = { owner: 'owner-pattern', repo: 'repo-pattern' } as never;

    expect(component.effectiveWorkflowPattern()).toBe('owner-pattern');
  });
});
