import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DeployWorkflowStatus } from 'src/app/shared/models';

/** Shows a deploy workflow status as an icon: spinner while in progress, check when successful, error on failure. */
@Component({
  selector: 'app-deploy-status-icon',
  templateUrl: './deploy-status-icon.component.html',
  styleUrls: ['./deploy-status-icon.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule, MatProgressSpinnerModule]
})
export class DeployStatusIconComponent {
  /** Status to display; a grey play icon is shown when it is missing. */
  @Input() status?: DeployWorkflowStatus;

  /** Tooltip text describing the status and the pull request being deployed. */
  get title(): string {
    if (!this.status) {
      return '';
    }

    const label = { InProgress: 'In progress', Successful: 'Successful', Failure: 'Failed' }[this.status.state];
    return `Deploy ${label} (#${this.status.pullNumber})`;
  }
}
