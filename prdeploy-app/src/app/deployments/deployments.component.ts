import { Component, DestroyRef, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  DeployEnvironment,
  DeployEnvironmentDeployGQL,
  DeployEnvironmentsAndQueuesGQL,
  DeployQueue
} from 'src/app/shared/graphql';
import { LoggingService } from 'src/app/shared/services';
import { QueueListComponent } from './queue-list/queue-list.component';
import { DxTemplateModule } from 'devextreme-angular/core';
import { DxAccordionModule, DxSelectBoxModule } from 'devextreme-angular';

import { EnvironmentListComponent } from './environment-list/environment-list.component';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { AddPrServiceDialogComponent } from './add-pr-service-dialog/add-pr-service-dialog.component';
import { NotificationManager, RepoManager, RouteManager } from '../shared/managers';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-deployments',
  templateUrl: './deployments.component.html',
  styleUrls: ['./deployments.component.scss'],
  standalone: true,
  imports: [
    DxButtonModule,
    DxSelectBoxModule,
    EnvironmentListComponent,
    DxAccordionModule,
    DxTemplateModule,
    QueueListComponent,
    AddPrServiceDialogComponent
  ]
})
export class DeploymentsComponent implements OnInit {
  deployEnvironments: DeployEnvironment[] = [];
  deployQueues: DeployQueue[] = [];
  loading = true;
  selectedQueueIndex = 0;
  addServiceToPrVisible = false;

  private _selectedEnvironment: string;

  constructor(
    public repoManager: RepoManager,
    private _routeManager: RouteManager,
    private _deployEnvironmentsAndQueuesGQL: DeployEnvironmentsAndQueuesGQL,
    private _deployEnvironmentDeployGQL: DeployEnvironmentDeployGQL,
    private _notificationManager: NotificationManager,
    private _loggingService: LoggingService,
    private _activatedRoute: ActivatedRoute,
    private _destroyRef: DestroyRef
  ) {}

  ngOnInit(): void {
    firstValueFrom(this._activatedRoute.queryParamMap).then(param => {
      this._selectedEnvironment = param.get('environment');
    });

    this.repoManager.valueChanged$.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(() => this.update());
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
      this.updateSelectedQueue();
      this._routeManager.updateQueryParams();
    } catch (error) {
      this._loggingService.error(error);
    }

    this.loading = false;
  }

  updateSelectedQueue(): void {
    if (this._selectedEnvironment) {
      this.selectedQueueIndex = this.deployQueues.findIndex(q => q.environment == this._selectedEnvironment);
    }
  }

  async queueUpdateStarted(): Promise<void> {
    this.loading = true;
  }

  async queueUpdateComplete(): Promise<void> {
    await this.update();
  }

  async selectedQueueChange(queue?: DeployQueue): Promise<void> {
    const environment = queue?.environment;
    this._selectedEnvironment = environment;
    this._routeManager.updateQueryParams({ environment: this._selectedEnvironment });
  }

  showAddServiceToPr(): void {
    this.addServiceToPrVisible = true;
  }
}
