import { Component, OnInit } from '@angular/core';
import { RepositoriesGridComponent } from './repositories-grid/repositories-grid.component';
import { EnabledOwnerReposGQL, OwnerRepoRemoveEnabledGQL, OwnerRepos } from '../shared/graphql';
import { firstValueFrom } from 'rxjs';
import { AddRepoDialogComponent } from './add-repo-dialog/add-repo-dialog.component';
import { Repository } from '../shared/models';
import { NotificationManager, RepoManager } from '../shared/managers';
import { LoggingService } from '../shared/services';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { AddRepoDialogData } from './add-repo-dialog/add-repo-dialog-data';
import { MtxDialog } from '@ng-matero/extensions/dialog';
import { AlertPanelComponent } from '../shared/components';

@Component({
  selector: 'app-repositories',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, RepositoriesGridComponent, AddRepoDialogComponent, AlertPanelComponent],
  templateUrl: './repositories.component.html',
  styleUrl: './repositories.component.scss'
})
export class RepositoriesComponent implements OnInit {
  constructor(
    private _enabledOwnerReposGQL: EnabledOwnerReposGQL,
    private _ownerRepoRemoveEnabledGQL: OwnerRepoRemoveEnabledGQL,
    private _notificationManager: NotificationManager,
    private _repoManager: RepoManager,
    private _dialog: MatDialog,
    private _mtxDialog: MtxDialog,
    private _loggingService: LoggingService
  ) {}

  ownerRepos: OwnerRepos[];
  loading = true;
  hasRepos = false;

  ngOnInit(): void {
    this.updateOwnerRepos();
  }

  async updateOwnerRepos(): Promise<void> {
    const response = await firstValueFrom(this._enabledOwnerReposGQL.fetch());
    this.ownerRepos = response.data.enabledOwnerRepos;
    this._repoManager.updateOwnerRepos(this.ownerRepos);
    this.ownerRepos = response.data.enabledOwnerRepos;
    this.hasRepos = this.ownerRepos.length > 0;
    this.loading = false;
  }

  async showAddRepo(owner: string = '') {
    const dialogRef = this._dialog.open<AddRepoDialogComponent, AddRepoDialogData, Repository>(AddRepoDialogComponent, {
      data: {
        owner
      },
      width: '450px',
      height: '300px'
    });

    const repository = await firstValueFrom(dialogRef.afterClosed());
    if (repository) {
      this.updateOwnerRepos();
    }
  }

  async removeRepo(repository: Repository) {
    this._mtxDialog.open({
      title: 'Delete Repository',
      description: `Are you sure you want to delete ${repository.owner}/${repository.repo}?`,
      buttons: [
        {
          text: 'Delete',
          onClick: async () => {
            try {
              await firstValueFrom(
                this._ownerRepoRemoveEnabledGQL.mutate({
                  input: repository
                })
              );

              this._notificationManager.show(`Repository ${repository.owner}/${repository.repo} removed.`);
              this.updateOwnerRepos();
            } catch (error) {
              this._loggingService.error(error, `Error removing repository.`);
              this._notificationManager.show('Error removing repository.', 'danger');
            }
          }
        },
        {
          text: 'Cancel'
        }
      ],
      width: '400px'
    });
  }
}
