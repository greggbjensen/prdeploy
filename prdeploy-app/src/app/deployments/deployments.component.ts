import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatExpansionModule, MatExpansionPanel } from '@angular/material/expansion';
import { firstValueFrom, switchMap, EMPTY } from 'rxjs';
import {
  DeployEnvironment,
  DeployEnvironmentDeployGQL,
  DeployEnvironmentsAndQueuesGQL,
  DeployQueue
} from 'src/app/shared/graphql';
import { DeployStatusService, LoggingService } from 'src/app/shared/services';
import { DeployWorkflowStatus } from 'src/app/shared/models';
import { QueueListComponent } from './queue-list/queue-list.component';
import { EnvironmentListComponent } from './environment-list/environment-list.component';
import { AddPrServiceDialogComponent } from './add-pr-service-dialog/add-pr-service-dialog.component';
import { NotificationManager, RepoManager, RouteManager } from '../shared/managers';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-deployments',
  templateUrl: './deployments.component.html',
  styleUrls: ['./deployments.component.scss'],
  imports: [
    MatButtonModule,
    MatIconModule,
    MatExpansionModule,
    MatProgressSpinnerModule,
    EnvironmentListComponent,
    QueueListComponent
  ],
  viewProviders: [MatExpansionPanel],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DeploymentsComponent implements OnInit {
  deployEnvironments: DeployEnvironment[] = [];
  deployQueues: DeployQueue[] = [];
  loading = true;
  /** Latest deploy workflow status keyed by lowercase environment name. */
  deployStatuses: Record<string, DeployWorkflowStatus> = {};
  selectedEnvironment: string;

  constructor(
    public repoManager: RepoManager,
    private _routeManager: RouteManager,
    private _deployEnvironmentsAndQueuesGQL: DeployEnvironmentsAndQueuesGQL,
    private _deployEnvironmentDeployGQL: DeployEnvironmentDeployGQL,
    private _notificationManager: NotificationManager,
    private _loggingService: LoggingService,
    private _deployStatusService: DeployStatusService,
    private _dialog: MatDialog,
    private _activatedRoute: ActivatedRoute,
    private _destroyRef: DestroyRef,
    private _changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.repoManager.valueChanged$.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(() => this.update());

    this.repoManager.valueChanged$
      .pipe(
        switchMap(() => {
          this.deployStatuses = {};
          return this.repoManager.isValid
            ? this._deployStatusService.watch(this.repoManager.owner, this.repoManager.repo)
            : EMPTY;
        }),
        takeUntilDestroyed(this._destroyRef)
      )
      .subscribe(status => this.applyDeployStatus(status));

    firstValueFrom(this._activatedRoute.queryParamMap).then(param => {
      this.selectedEnvironment = param.get('environment');
    });
  }

  /** Records a streamed deploy status and refreshes the view. A new object keeps OnPush children updating. */
  applyDeployStatus(status: DeployWorkflowStatus): void {
    this.deployStatuses = { ...this.deployStatuses, [status.environment.toLowerCase()]: status };
    this._changeDetectorRef.markForCheck();
  }

  async triggerDeployments(event: MouseEvent, queue: DeployQueue): Promise<void> {
    event.stopPropagation();

    try {
      // Re-trigger first item in queue.
      const pullRequest = queue.pullRequests[0];
      await firstValueFrom(
        this._deployEnvironmentDeployGQL.mutate({
          variables: {
            input: {
              owner: this.repoManager.owner,
              repo: this.repoManager.repo,
              environment: queue.environment,
              pullNumber: pullRequest.number
            }
          }
        })
      );

      this._notificationManager.show(`Deploy ${queue.environment} comment added, it may take a minute to update.`);
    } catch (error) {
      this._loggingService.error(error);
    }
  }

  async update(): Promise<void> {
    try {
      if (!this.repoManager.owner || !this.repoManager.repo) {
        return;
      }

      this.loading = true;

      const response = await firstValueFrom(
        this._deployEnvironmentsAndQueuesGQL.fetch({
          variables: {
            input: {
              owner: this.repoManager.owner,
              repo: this.repoManager.repo
            }
          }
        })
      );
      this.deployEnvironments = response.data.deployEnvironments;
      if (!this.selectedEnvironment && this.deployEnvironments.length > 0) {
        this.selectedEnvironment = this.deployEnvironments[0].name;
      }

      this.deployQueues = response.data.deployQueues;
      this._routeManager.updateQueryParams({ environment: this.selectedEnvironment });
    } catch (error) {
      this._loggingService.error(error);
    }

    this.loading = false;
    this._changeDetectorRef.detectChanges();
  }

  async queueUpdateStarted(): Promise<void> {
    this.loading = true;
  }

  async queueUpdateComplete(): Promise<void> {
    await this.update();
  }

  async selectedQueueChange(queue?: DeployQueue): Promise<void> {
    const environment = queue?.environment;
    this.selectedEnvironment = environment;
    if (environment) {
      this._routeManager.updateQueryParams({ environment: this.selectedEnvironment });
    }
  }

  showAddServiceToPr(): void {
    this._dialog.open<AddPrServiceDialogComponent>(AddPrServiceDialogComponent, {
      width: '500px',
      height: '480px'
    });
  }
}
