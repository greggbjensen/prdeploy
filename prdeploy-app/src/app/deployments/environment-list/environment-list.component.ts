import { AfterViewInit, Component, Input, ViewChild } from '@angular/core';
import { DxDataGridComponent, DxDataGridModule } from 'devextreme-angular';
import { DeployEnvironment, DeployEnvironmentDeployGQL, DeployEnvironmentFreeGQL } from 'src/app/shared/graphql';
import { firstValueFrom } from 'rxjs';
import { LoggingService } from 'src/app/shared/services';
import { PullRequestPopoverComponent } from '../pull-request-popover/pull-request-popover.component';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DatePipe } from '@angular/common';
import { DxTemplateModule } from 'devextreme-angular/core';
import { DxiColumnModule, DxoLoadPanelModule } from 'devextreme-angular/ui/nested';
import { DeployRollbackDialogComponent } from '../deploy-rollback-dialog/deploy-rollback-dialog.component';
import { DeployForceDialogComponent } from '../deploy-force-dialog/deploy-force-dialog.component';
import { NotificationManager, RepoManager } from 'src/app/shared/managers';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-environment-list',
  templateUrl: './environment-list.component.html',
  styleUrls: ['./environment-list.component.scss'],
  standalone: true,
  imports: [
    DxDataGridModule,
    DxiColumnModule,
    DxoLoadPanelModule,
    DxTemplateModule,
    DxButtonModule,
    PullRequestPopoverComponent,
    DeployForceDialogComponent,
    DeployRollbackDialogComponent,
    DatePipe,
    RouterModule
  ]
})
export class EnvironmentListComponent implements AfterViewInit {
  @Input() data: DeployEnvironment[] = [];

  @Input() set loading(value: boolean) {
    this._loading = value;
    this.forceDeployVisible = false;
    if (this.environmentDataGrid) {
      if (value) {
        this.environmentDataGrid.instance.beginCustomLoading('Loading...');
      } else {
        this.environmentDataGrid.instance.endCustomLoading();
      }
    }
  }

  actionEnvironment: string;
  actionPullNumber: number;
  forceDeployVisible: boolean;
  deployRollbackVisible: boolean;

  get loading(): boolean {
    return this._loading;
  }

  @ViewChild('environmentDataGrid') environmentDataGrid: DxDataGridComponent | undefined;

  private _loading = false;

  constructor(
    public repoManager: RepoManager,
    private _deployEnvironmentDeployGQL: DeployEnvironmentDeployGQL,
    private _deployEnvironmentFreeGQL: DeployEnvironmentFreeGQL,
    private _notificationManager: NotificationManager,
    private _loggingService: LoggingService
  ) {}

  ngAfterViewInit(): void {
    this.loading = true;
  }

  async free(environment: string, pullNumber: number): Promise<void> {
    try {
      await firstValueFrom(
        this._deployEnvironmentFreeGQL.mutate({
          input: {
            owner: this.repoManager.owner,
            repo: this.repoManager.repo,
            environment,
            pullNumber
          }
        })
      );

      this._notificationManager.show(`Free ${environment} comment added, it may take a minute to update.`);
    } catch (error) {
      this._loggingService.error(error);
    }
  }

  showDeployForce(environment: string): void {
    this.forceDeployVisible = true;
    this.actionEnvironment = environment;
  }

  showDeployRollback(environment: string, pullNumber: number): void {
    this.deployRollbackVisible = true;
    this.actionPullNumber = pullNumber;
    this.actionEnvironment = environment;
  }

  async redeploy(environment: string, pullNumber: number): Promise<void> {
    try {
      await firstValueFrom(
        this._deployEnvironmentDeployGQL.mutate({
          input: {
            owner: this.repoManager.owner,
            repo: this.repoManager.repo,
            environment,
            pullNumber
          }
        })
      );

      this._notificationManager.show(`Redeploy ${environment} comment added, it may take a minute to update.`);
    } catch (error) {
      this._loggingService.error(error);
    }
  }
}
