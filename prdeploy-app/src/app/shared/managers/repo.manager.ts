import { Injectable } from '@angular/core';
import { Location } from '@angular/common';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { BehaviorSubject, firstValueFrom } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class RepoManager {
  private _repo = '';
  private _owner = '';

  private _valueChangedSubject$ = new BehaviorSubject(false);
  valueChanged$ = this._valueChangedSubject$.asObservable();

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
    return this._valueChangedSubject$.value;
  }

  get repo(): string {
    return this._repo;
  }

  set repo(value: string) {
    if (value !== this.repo) {
      this._repo = value;
      this.updateQueryParams();
      this.updateValueChanged();
    }
  }

  get owner(): string {
    return this._owner;
  }

  set owner(value: string) {
    if (value !== this.owner) {
      this._owner = value;
      this.updateQueryParams();
      this.updateValueChanged();
    }
  }

  async updateQueryParams(additionalParams: Params = {}): Promise<void> {
    // Do not update while login is in process.
    if (this.location.path().startsWith('/login')) {
      return;
    }

    await this._router.navigate([this.location.path()], {
      queryParams: {
        owner: this.owner,
        repo: this.repo,
        ...additionalParams
      },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  private updateValueChanged() {
    const isValid = !!this.repo && !!this.owner;
    if (isValid) {
      this._valueChangedSubject$.next(true);
    }
  }
}
