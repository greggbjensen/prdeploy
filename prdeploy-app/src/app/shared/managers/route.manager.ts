import { DestroyRef, Injectable } from '@angular/core';
import { Params, Router } from '@angular/router';
import { Location } from '@angular/common';
import { RepoManager } from './repo.manager';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Injectable({
  providedIn: 'root'
})
export class RouteManager {
  constructor(
    private _location: Location,
    private _router: Router,
    private _repoManager: RepoManager,
    private _destroyRef: DestroyRef
  ) {
    this._repoManager.valueChanged$
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe(() => this.updateQueryParams());
  }

  async updateQueryParams(additionalParams: Params = {}): Promise<void> {
    // Do not update while login is in process.
    if (this._location.path().startsWith('/login')) {
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
}
