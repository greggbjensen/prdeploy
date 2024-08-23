import { Injectable } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { RepoManager } from './repo.manager';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { OwnerRepos } from '../graphql';
import { firstValueFrom } from 'rxjs';
import _ from 'lodash';

@Injectable({
  providedIn: 'root'
})
export class RouteManager {
  constructor(
    private _router: Router,
    private _activatedRoute: ActivatedRoute,
    private _repoManager: RepoManager
  ) {
    firstValueFrom(this._activatedRoute.queryParamMap).then(param => {
      this._repoManager.repo = param.get('repo');
      this._repoManager.owner = param.get('owner');
    });
    this._repoManager.valueChanged$.pipe(takeUntilDestroyed()).subscribe(() => this.updateQueryParams());
    this._repoManager.ownerReposChanged$
      .pipe(takeUntilDestroyed())
      .subscribe(ownerRepos => this.checkHasRepos(ownerRepos));
  }

  async navigate(commands: any[], additionalParams: Params = {}): Promise<void> {
    await this._router.navigate(commands, {
      queryParams: {
        owner: this._repoManager.owner,
        repo: this._repoManager.repo,
        ...additionalParams
      },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  async updateQueryParams(additionalParams: Params = {}): Promise<void> {
    // Do not update while login is in process.
    const path = this._router.url.split('?')[0];
    if (!path || path.startsWith('/login') || path.startsWith('/repositories')) {
      return;
    }

    await this._router.navigate([path], {
      queryParams: {
        owner: this._repoManager.owner,
        repo: this._repoManager.repo,
        ...additionalParams
      },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  private async checkHasRepos(ownerRepos: OwnerRepos[]) {
    // If there are no repos yet, send to that page.
    const path = this._router.url.split('?')[0];
    if (!path) {
      return;
    }

    if (
      this._repoManager.isValid &&
      ownerRepos &&
      ownerRepos.length === 0 &&
      !path.startsWith('/repositories') &&
      !path.startsWith('/login')
    ) {
      this._router.navigate(['/repositories'], { queryParams: { addrepos: true } });
    }
  }
}
