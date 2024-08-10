import { DestroyRef, Injectable } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { Location } from '@angular/common';
import { RepoManager } from './repo.manager';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { OwnerRepos } from '../graphql';
import { firstValueFrom } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class RouteManager {
  constructor(
    private _location: Location,
    private _router: Router,
    private _activatedRoute: ActivatedRoute,
    private _repoManager: RepoManager,
    private _destroyRef: DestroyRef
  ) {
    firstValueFrom(this._activatedRoute.queryParamMap).then(param => {
      this._repoManager.repo = param.get('repo');
      this._repoManager.owner = param.get('owner');
    });
    this._repoManager.valueChanged$
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe(() => this.updateQueryParams());
    this._repoManager.ownerReposChanged$
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe(ownerRepos => this.checkHasRepos(ownerRepos));
  }

  async navigate(commands: any[], additionalParams: Params = {}): Promise<void> {
    await this._router.navigate([commands], {
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
    if (this._location.path().startsWith('/login') || this._location.path().startsWith('/repositories')) {
      return;
    }

    await this._router.navigate([this._location.path()], {
      queryParams: {
        owner: this._repoManager.owner,
        repo: this._repoManager.repo,
        ...additionalParams
      },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  private checkHasRepos(ownerRepos: OwnerRepos[]) {
    // If there are no repos yet, send to that page.
    if (
      this._repoManager.isValid &&
      ownerRepos &&
      ownerRepos.length === 0 &&
      !this._location.path().startsWith('/repositories') &&
      !this._location.path().startsWith('/login')
    ) {
      this._router.navigate(['/repositories'], { queryParams: { addrepos: true } });
    }
  }
}
