import { AfterViewInit, Component, Input } from '@angular/core';
import { DeployEnvironment, DeployEnvironmentDeployGQL, DeployEnvironmentFreeGQL } from 'src/app/shared/graphql';
import { firstValueFrom } from 'rxjs';
import { LoggingService } from 'src/app/shared/services';
import { DatePipe } from '@angular/common';
import { DeployRollbackDialogComponent } from './deploy-rollback-dialog/deploy-rollback-dialog.component';
import { DeployForceDialogComponent } from './deploy-force-dialog/deploy-force-dialog.component';
import { NotificationManager, RepoManager } from 'src/app/shared/managers';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatTableModule } from '@angular/material/table';
import { DeployForceDialogData } from './deploy-force-dialog/deploy-force-dialog-data';
import { DeployRollbackDialogData } from './deploy-rollback-dialog/deploy-rollback-dialog-data';
import { MtxPopoverModule } from '@ng-matero/extensions/popover';
import { MarkdownComponent } from 'ngx-markdown';
import { CleanMarkdownPipe } from 'src/app/shared/pipes';

@Component({
  selector: 'app-environment-list',
  templateUrl: './environment-list.component.html',
  styleUrls: ['./environment-list.component.scss'],
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    DeployForceDialogComponent,
    DeployRollbackDialogComponent,
    MtxPopoverModule,
    MarkdownComponent,
    DatePipe,
    CleanMarkdownPipe,
    RouterModule
  ]
})
export class EnvironmentListComponent implements AfterViewInit {
  @Input() data: DeployEnvironment[] = [];

  @Input() set loading(value: boolean) {
    this._loading = value;
  }

  get loading(): boolean {
    return this._loading;
  }

  displayedColumns: string[] = ['environment', 'locked', 'pull-request', 'updated-at', 'user', 'url', 'actions'];

  private _loading = false;

  constructor(
    public repoManager: RepoManager,
    private _deployEnvironmentDeployGQL: DeployEnvironmentDeployGQL,
    private _deployEnvironmentFreeGQL: DeployEnvironmentFreeGQL,
    private _notificationManager: NotificationManager,
    private _loggingService: LoggingService,
    private _dialog: MatDialog
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
    this._dialog.open<DeployForceDialogComponent, DeployForceDialogData>(DeployForceDialogComponent, {
      data: {
        repository: this.repoManager,
        environment
      },
      width: '550px',
      height: '350px'
    });
  }

  showDeployRollback(environment: string, pullNumber: number): void {
    this._dialog.open<DeployRollbackDialogComponent, DeployRollbackDialogData>(DeployRollbackDialogComponent, {
      data: {
        pullNumber,
        environment,
        repository: this.repoManager
      },
      width: '400px',
      height: '260px'
    });
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
