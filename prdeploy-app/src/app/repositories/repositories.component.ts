import { Component, OnInit } from '@angular/core';
import { RepositoriesGridComponent } from './repositories-grid/repositories-grid.component';
import { EnabledOwnerReposGQL, OwnerRepoRemoveEnabledGQL, OwnerRepos } from '../shared/graphql';
import { firstValueFrom } from 'rxjs';
import { DxButtonModule } from 'devextreme-angular';
import { confirm } from 'devextreme/ui/dialog';
import { AddRepoDialogComponent } from './add-repo-dialog/add-repo-dialog.component';
import { Repository } from '../shared/models';
import { NotificationManager, RepoManager } from '../shared/managers';
import { LoggingService } from '../shared/services';

@Component({
  selector: 'app-repositories',
  standalone: true,
  imports: [DxButtonModule, RepositoriesGridComponent, AddRepoDialogComponent],
  templateUrl: './repositories.component.html',
  styleUrl: './repositories.component.scss'
})
export class RepositoriesComponent implements OnInit {
  constructor(
    private _enabledOwnerReposGQL: EnabledOwnerReposGQL,
    private _ownerRepoRemoveEnabledGQL: OwnerRepoRemoveEnabledGQL,
    private _notificationManager: NotificationManager,
    private _repoManager: RepoManager,
    private _loggingService: LoggingService
  ) {}

  ownerRepos: OwnerRepos[];
  addRepoVisible = false;
  addRepoOwner = '';

  ngOnInit(): void {
    this.updateOwnerRepos();
  }

  async updateOwnerRepos(): Promise<void> {
    const response = await firstValueFrom(this._enabledOwnerReposGQL.fetch());
    this.ownerRepos = response.data.enabledOwnerRepos;
    this._repoManager.updateOwnerRepos(this.ownerRepos);
    this.ownerRepos = response.data.enabledOwnerRepos;
  }

  showAddRepo(owner: string = ''): void {
    this.addRepoOwner = owner;
    this.addRepoVisible = true;
  }

  async removeRepo(repository: Repository) {
    const confirmed = await confirm(
      `Are you sure you want to delete ${repository.owner}/${repository.repo}?`,
      'Delete Repository'
    );
    if (!confirmed) {
      return;
    }

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
      this._notificationManager.show('Error removing repository.', 'error');
    }
  }
}
