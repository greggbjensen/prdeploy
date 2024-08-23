import { Injectable } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { RepoManager } from './repo.manager';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';
import _ from 'lodash';

@Injectable({
  providedIn: 'root'
})
export class RouteManager {
  private static readonly StartingSlackRegex = /^\//;

  constructor(
    private _router: Router,
    private _activatedRoute: ActivatedRoute,
    private _repoManager: RepoManager
  ) {
    firstValueFrom(this._activatedRoute.params).then(params => {
      this._repoManager.repo = params['repo'] || '';
      this._repoManager.owner = params['owner'] || '';
    });
    this._repoManager.valueChanged$.pipe(takeUntilDestroyed()).subscribe(repository => {
      if (
        repository &&
        repository.owner &&
        repository.repo &&
        repository.owner.length > 0 &&
        repository.repo.length > 0
      ) {
        this.updateOwnerRepo(repository.owner, repository.repo);
      }
    });
  }

  async navigate(commands: any[], queryParams: Params = {}): Promise<void> {
    await this._router.navigate(commands, {
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  async updateQueryParams(queryParams: Params = {}): Promise<void> {
    // Do not update while login is in process.
    const path = this._router.url.split('?')[0];
    if (!path || path.startsWith('/login') || path.startsWith('/repositories')) {
      return;
    }

    await this._router.navigate([path], {
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  private async updateOwnerRepo(owner: string, repo: string) {
    // Do not update paths that down have owner and repo at the start.
    const path = this._router.url.split('?')[0];
    if (!path || !owner || !repo || path.startsWith('/login') || path.startsWith('/repositories')) {
      return;
    }

    const parts = path.replace(RouteManager.StartingSlackRegex, '').split('/');
    if (parts.length > 2) {
      parts.splice(0, 2);
      this.navigate([owner, repo, ...parts]);
    }
  }
}
