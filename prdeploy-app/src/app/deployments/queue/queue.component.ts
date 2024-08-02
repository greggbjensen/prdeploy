import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SelectionChangedEvent } from 'devextreme/ui/select_box';
import { first, firstValueFrom } from 'rxjs';
import {
  DeployEnvironment,
  DeployEnvironmentDeployGQL,
  DeployEnvironmentsAndQueuesGQL,
  DeployQueue,
  PrDeployEnabledRepositoriesGQL,
  Repository
} from 'src/app/shared/graphql';
import { DialogButton, DialogService, LoggingService, StatusDialogType } from 'src/app/shared/services';
import { QueueListComponent } from './queue-list/queue-list.component';
import { DxTemplateModule } from 'devextreme-angular/core';
import { DxAccordionModule, DxSelectBoxModule } from 'devextreme-angular';

import { EnvironmentListComponent } from './environment-list/environment-list.component';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { AddPrServiceDialogComponent } from './add-pr-service-dialog/add-pr-service-dialog.component';
import { uniq } from 'lodash';

@Component({
  selector: 'app-queue',
  templateUrl: './queue.component.html',
  styleUrls: ['./queue.component.scss'],
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
export class QueueComponent implements OnInit {
  deployEnvironments: DeployEnvironment[] = [];
  deployQueues: DeployQueue[] = [];
  loading = true;
  selectedQueueIndex = 0;
  addServiceToPrVisible = false;
  owners: string[];
  repos: string[];
  repository: Repository = {
    owner: '',
    repo: ''
  };

  private _repositories: Repository[] = [];
  private _selectedEnvironment: string;

  constructor(
    private _deployEnvironmentsAndQueuesGQL: DeployEnvironmentsAndQueuesGQL,
    private _deployEnvironmentDeployGQL: DeployEnvironmentDeployGQL,
    private _prDeployEnabledRepositoriesGQL: PrDeployEnabledRepositoriesGQL,
    private _dialogService: DialogService,
    private _loggingService: LoggingService,
    private _route: ActivatedRoute,
    private _router: Router
  ) {}

  ngOnInit(): void {
    firstValueFrom(this._route.queryParamMap).then(param => {
      this._selectedEnvironment = param.get('environment');
      this.repository.owner = param.get('owner');
      this.repository.repo = param.get('repo');
    });

    this._prDeployEnabledRepositoriesGQL
      .fetch()
      .pipe(first())
      .subscribe(r => {
        this._repositories = r.data.prDeployEnabledRepositories;
        this.owners = uniq(this._repositories.map(r => r.owner));
        if (!this.repository.owner || this.owners.includes(this.repository.owner.toLowerCase())) {
          this.repository.owner = this.owners[0];
        }

        this.updateOwnerRepos();
        this.update();
      });
  }

  async triggerDeployments(queue: DeployQueue): Promise<void> {
    try {
      // Re-trigger first item in queue.
      const pullRequest = queue.pullRequests[0];
      await firstValueFrom(
        this._deployEnvironmentDeployGQL.mutate({
          owner: this.repository.owner,
          repo: this.repository.repo,
          environment: queue.environment,
          pullRequestNumber: pullRequest.number
        })
      );

      await firstValueFrom(
        this._dialogService.showStatusDialog(
          StatusDialogType.Success,
          `Deploy ${queue.environment} Started`,
          [
            `The comment to deploy from the ${queue.environment} environment has been added.`,
            'It may take a minute to update.'
          ],
          [new DialogButton('OK', 'primary')]
        )
      );
    } catch (error) {
      this._loggingService.error(error);
    }
  }

  async update(): Promise<void> {
    this.loading = true;

    try {
      const response = await firstValueFrom(
        this._deployEnvironmentsAndQueuesGQL.fetch({
          owner: this.repository.owner,
          repo: this.repository.repo
        })
      );
      this.deployEnvironments = response.data.deployEnvironments;
      this.deployQueues = response.data.deployQueues;
      this.updateSelectedQueue();
      this.updateNavigationUrl();
    } catch (error) {
      this._loggingService.error(error);
    }

    this.loading = false;
  }

  async selectedRepoChanged(event: SelectionChangedEvent): Promise<void> {
    this.repository.repo = event.selectedItem;
    await this.update();
  }

  async selectedOwnerChanged(event: SelectionChangedEvent): Promise<void> {
    this.repository.owner = event.selectedItem;
    this.updateOwnerRepos();
    await this.update();
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
    this.updateNavigationUrl();
  }

  updateNavigationUrl(): void {
    this._router.navigate([], {
      queryParams: {
        environment: this._selectedEnvironment,
        owner: this.repository.owner,
        repo: this.repository.repo
      },
      replaceUrl: true
    });
  }

  showAddServiceToPr(): void {
    this.addServiceToPrVisible = true;
  }

  repositoryDisplayExpr(item: Repository) {
    return item ? `${item.owner}/${item.repo}` : '';
  }

  private updateOwnerRepos() {
    this.repos = this._repositories.filter(r => r.owner === this.repository.owner).map(r => r.repo);
    if (!this.repository.repo || !this.repos.includes(this.repository.repo.toLowerCase())) {
      this.repository.repo = this.repos ? this.repos[0] : null;
    }
  }
}
