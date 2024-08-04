import { EventEmitter, Injectable } from '@angular/core';
import { Location } from '@angular/common';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { BehaviorSubject, firstValueFrom } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class RepoManager {
  private _repoSubject$ = new BehaviorSubject('');
  private _ownerSubject$ = new BehaviorSubject('');
  private _isValidSubject$ = new BehaviorSubject(false);

  repo$ = this._repoSubject$.asObservable();
  owner$ = this._ownerSubject$.asObservable();
  isValid$ = this._isValidSubject$.asObservable();

  constructor(
    private _router: Router,
    private _activatedRoute: ActivatedRoute,
    private location: Location
  ) {
    firstValueFrom(this._activatedRoute.queryParamMap).then(param => {
      this.repo = param.get('repo');
      this.owner = param.get('owner');
    });
  }

  get isValid(): boolean {
    return this._isValidSubject$.value;
  }

  get repo(): string {
    return this._repoSubject$.value;
  }

  set repo(value: string) {
    if (value !== this.repo) {
      this._repoSubject$.next(value);
      this.updateQueryParams();
      this.updateIsValid();
    }
  }

  get owner(): string {
    return this._ownerSubject$.value;
  }

  set owner(value: string) {
    if (value !== this.owner) {
      this._ownerSubject$.next(value);
      this.updateQueryParams();
      this.updateIsValid();
    }
  }

  async updateQueryParams(additionalParams: Params = {}): Promise<void> {
    await this._router.navigate([this.location.path()], {
      relativeTo: this._activatedRoute,
      queryParams: {
        owner: this.owner,
        repo: this.repo,
        ...additionalParams
      },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  private updateIsValid() {
    const isValid = !!this.repo && !!this.owner;
    if (isValid != this.isValid) {
      this._isValidSubject$.next(isValid);
    }
  }
}
