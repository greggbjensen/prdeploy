import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DeployWorkflowState, DeployWorkflowStatus } from 'src/app/shared/models';
import { DeployStatusIconComponent } from './deploy-status-icon.component';

/** Verifies the icon shown for each deploy workflow state. */
describe('DeployStatusIconComponent', () => {
  let fixture: ComponentFixture<DeployStatusIconComponent>;

  /** Builds a status in the given state. */
  function status(
    state: DeployWorkflowState,
    runUrl: string | null = 'https://example.test/run'
  ): DeployWorkflowStatus {
    return { owner: 'o', repo: 'r', environment: 'dev', state, pullNumber: 7, runId: 1, runUrl, updatedAt: '' };
  }

  /** Renders the component with a status. */
  function render(value?: DeployWorkflowStatus): HTMLElement {
    fixture.componentRef.setInput('status', value);
    fixture.detectChanges();
    return fixture.nativeElement;
  }

  beforeEach(() => {
    fixture = TestBed.createComponent(DeployStatusIconComponent);
  });

  it('shows a spinner while in progress', () => {
    const el = render(status('InProgress'));
    expect(el.querySelector('mat-spinner')).not.toBeNull();
    expect(el.querySelector('a')?.title).toBe('Deploy In progress (#7)');
    expect(el.querySelector('a')?.getAttribute('href')).toBe('https://example.test/run');
    expect(el.querySelector('a')?.getAttribute('target')).toBe('_blank');
  });

  it('shows a check icon when successful', () => {
    expect(render(status('Successful')).querySelector('.deploy-status-successful')?.textContent).toContain(
      'check_circle'
    );
  });

  it('shows an error icon on failure and links to the run', () => {
    const el = render(status('Failure'));
    expect(el.querySelector('.deploy-status-failure')?.textContent).toContain('error');
    expect(el.querySelector('a')?.getAttribute('href')).toBe('https://example.test/run');
    expect(el.querySelector('a')?.title).toBe('Deploy Failed (#7)');
  });

  it('omits the link when the run url is unknown', () => {
    expect(render(status('Failure', null)).querySelector('a')?.hasAttribute('href')).toBeFalse();
  });

  it('shows a grey play icon without a status', () => {
    const el = render(undefined);
    expect(el.querySelector('.deploy-status-none')?.textContent).toContain('play_circle_outline');
    expect(el.querySelector('a')).toBeNull();
    expect(el.querySelector('.deploy-status')?.getAttribute('title')).toBe('No deploy status yet');
  });
});
