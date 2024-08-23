import { Component } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { firstValueFrom } from 'rxjs';
import { EnabledOwnerReposGQL } from 'src/app/shared/graphql';
import { RepoManager, RouteManager } from 'src/app/shared/managers';

@Component({
  selector: 'app-secure-redirect',
  standalone: true,
  imports: [MatProgressSpinnerModule],
  templateUrl: './secure-redirect.component.html',
  styleUrl: './secure-redirect.component.scss'
})
export class SecureRedirectComponent {
  constructor(
    private _repoManager: RepoManager,
    private _routeManager: RouteManager,
    private _enabledOwnerReposGQL: EnabledOwnerReposGQL
  ) {
    this.redirectToOwnerRepo();
  }

  async redirectToOwnerRepo() {
    const response = await firstValueFrom(this._enabledOwnerReposGQL.fetch());
    const ownerRepos = response.data.enabledOwnerRepos;
    const hasRepos = await this._repoManager.updateOwnerRepos(ownerRepos);
    if (hasRepos && ownerRepos && ownerRepos.length > 0) {
      const owner = ownerRepos[0].owner;
      const repo = ownerRepos[0].repos[0];
      this._routeManager.navigate(['/', owner, repo, 'deployments']);
    }
  }
}
