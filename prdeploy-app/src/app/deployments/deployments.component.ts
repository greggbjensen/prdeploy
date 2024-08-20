import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatExpansionModule, MatExpansionPanel } from '@angular/material/expansion';
import { firstValueFrom } from 'rxjs';
import {
  DeployEnvironment,
  DeployEnvironmentDeployGQL,
  DeployEnvironmentsAndQueuesGQL,
  DeployQueue
} from 'src/app/shared/graphql';
import { LoggingService } from 'src/app/shared/services';
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
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatExpansionModule,
    MatProgressSpinnerModule,
    EnvironmentListComponent,
    QueueListComponent,
    AddPrServiceDialogComponent
  ],
  viewProviders: [MatExpansionPanel],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DeploymentsComponent implements OnInit {
  deployEnvironments: DeployEnvironment[] = [];
  deployQueues: DeployQueue[] = [];
  loading = true;
  selectedEnvironment: string;

  constructor(
    public repoManager: RepoManager,
    private _routeManager: RouteManager,
    private _deployEnvironmentsAndQueuesGQL: DeployEnvironmentsAndQueuesGQL,
    private _deployEnvironmentDeployGQL: DeployEnvironmentDeployGQL,
    private _notificationManager: NotificationManager,
    private _loggingService: LoggingService,
    private _dialog: MatDialog,
    private _activatedRoute: ActivatedRoute,
    private _changeDetectorRef: ChangeDetectorRef
  ) {
    this.repoManager.valueChanged$.pipe(takeUntilDestroyed()).subscribe(() => this.update());
  }

  ngOnInit(): void {
    firstValueFrom(this._activatedRoute.queryParamMap).then(param => {
      this.selectedEnvironment = param.get('environment');
    });
  }

  async triggerDeployments(queue: DeployQueue): Promise<void> {
    try {
      // Re-trigger first item in queue.
      const pullRequest = queue.pullRequests[0];
      await firstValueFrom(
        this._deployEnvironmentDeployGQL.mutate({
          input: {
            owner: this.repoManager.owner,
            repo: this.repoManager.repo,
            environment: queue.environment,
            pullNumber: pullRequest.number
          }
        })
      );

      this._notificationManager.show(`Deploy ${queue.environment} comment added, it may take a minute to update.`);
    } catch (error) {
      this._loggingService.error(error);
    }
  }

  async update(): Promise<void> {
    if (!this.repoManager.isValid) {
      return;
    }

    this.loading = true;
    this._changeDetectorRef.detectChanges();

    try {
      const response = await firstValueFrom(
        this._deployEnvironmentsAndQueuesGQL.fetch({
          input: {
            owner: this.repoManager.owner,
            repo: this.repoManager.repo
          }
        })
      );
      this.deployEnvironments = response.data.deployEnvironments;
      this.deployQueues = response.data.deployQueues;
      this._routeManager.updateQueryParams();
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
      height: '320px'
    });
  }
}
