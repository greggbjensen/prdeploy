import { AfterViewInit, Component, Input, ViewChild } from '@angular/core';
import { DxDataGridComponent, DxDataGridModule } from 'devextreme-angular';
// import {
//   DeployEnvironment,
//   DeployEnvironmentDeployGQL,
//   DeployEnvironmentFreeGQL,
//   Repository
// } from 'src/app/shared/graphql';
import { firstValueFrom } from 'rxjs';
import { DialogButton, DialogService, LoggingService, StatusDialogType } from 'src/app/shared/services';
import { PullRequestPopoverComponent } from '../pull-request-popover/pull-request-popover.component';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DatePipe } from '@angular/common';
import { DxTemplateModule } from 'devextreme-angular/core';
import { DxiColumnModule, DxoLoadPanelModule } from 'devextreme-angular/ui/nested';
import { DeployRollbackDialogComponent } from '../deploy-rollback-dialog/deploy-rollback-dialog.component';
import { DeployForceDialogComponent } from '../deploy-force-dialog/deploy-force-dialog.component';

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
    DatePipe
  ]
})
export class EnvironmentListComponent implements AfterViewInit {
  // @Input() repository: Repository;
  // @Input() data: DeployEnvironment[] = [];

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
  actionPullNumber: string;
  forceDeployVisible: boolean;
  deployRollbackVisible: boolean;

  get loading(): boolean {
    return this._loading;
  }

  @ViewChild('environmentDataGrid') environmentDataGrid: DxDataGridComponent | undefined;

  private _loading = false;

  constructor(
    // private _deployEnvironmentDeployGQL: DeployEnvironmentDeployGQL,
    // private _deployEnvironmentFreeGQL: DeployEnvironmentFreeGQL,
    private _dialogService: DialogService,
    private _loggingService: LoggingService
  ) {}

  ngAfterViewInit(): void {
    this.loading = true;
  }

  async free(environment: string, pullRequestNumber: string): Promise<void> {
    try {
      // await firstValueFrom(
      //   this._deployEnvironmentFreeGQL.mutate({
      //     owner: this.repository.owner,
      //     repo: this.repository.repo,
      //     environment,
      //     pullRequestNumber
      //   })
      // );

      await firstValueFrom(
        this._dialogService.showStatusDialog(
          StatusDialogType.Success,
          `Free ${environment} Started`,
          [`The comment to free the ${environment} environment has been added.`, 'It may take a minute to update.'],
          [new DialogButton('OK', 'primary')]
        )
      );
    } catch (error) {
      this._loggingService.error(error);
    }
  }

  showDeployForce(environment: string): void {
    this.forceDeployVisible = true;
    this.actionEnvironment = environment;
  }

  showDeployRollback(environment: string, pullNumber: string): void {
    this.deployRollbackVisible = true;
    this.actionPullNumber = pullNumber;
    this.actionEnvironment = environment;
  }

  async redeploy(environment: string, pullRequestNumber: string): Promise<void> {
    try {
      // await firstValueFrom(
      //   this._deployEnvironmentDeployGQL.mutate({
      //     owner: this.repository.owner,
      //     repo: this.repository.repo,
      //     environment,
      //     pullRequestNumber
      //   })
      // );

      await firstValueFrom(
        this._dialogService.showStatusDialog(
          StatusDialogType.Success,
          `Redeploy ${environment} Started`,
          [`The comment to redeploy from the ${environment} environment has been added.`],
          [new DialogButton('OK', 'primary')]
        )
      );
    } catch (error) {
      this._loggingService.error(error);
    }
  }
}