import { Injectable } from '@angular/core';
import { BehaviorSubject, firstValueFrom } from 'rxjs';
import { EnabledOwnerReposGQL, OwnerRepos } from '../graphql';
import _ from 'lodash';
import { Router } from '@angular/router';
import { Repository } from '../models';

@Injectable({
  providedIn: 'root'
})
export class RepoManager {
  private _isLoaded = false;

  private _ownerReposChangedSubject = new BehaviorSubject<OwnerRepos[]>([]);
  ownerReposChanged$ = this._ownerReposChangedSubject.asObservable();

  private _valueChangedSubject = new BehaviorSubject<Repository>(null);
  valueChanged$ = this._valueChangedSubject.asObservable();

  get isValid(): boolean {
    return (
      this._valueChangedSubject.value &&
      this._valueChangedSubject.value.owner &&
      this._valueChangedSubject.value.owner.length > 0 &&
      this._valueChangedSubject.value.repo &&
      this._valueChangedSubject.value.repo.length > 0
    );
  }

  get hasOwnerRepos(): boolean {
    return (
      this._ownerReposChangedSubject.value &&
      this._ownerReposChangedSubject.value.length > 0 &&
      this._ownerReposChangedSubject.value[0].repos.length > 0
    );
  }

  get repo(): string {
    return this._valueChangedSubject.value ? this._valueChangedSubject.value.repo : '';
  }

  set repo(value: string) {
    if (value !== this.repo) {
      this.updateValueChanged(this.owner, value);
    }
  }

  get owner(): string {
    return this._valueChangedSubject.value ? this._valueChangedSubject.value.owner : '';
  }

  set owner(value: string) {
    if (value !== this.owner) {
      this.updateValueChanged(value, this.repo);
    }
  }

  constructor(
    private _enabledOwnerReposGQL: EnabledOwnerReposGQL,
    private _router: Router
  ) {}

  async fetchOwnerRepos() {
    const result = await firstValueFrom(this._enabledOwnerReposGQL.fetch());
    this._isLoaded = true;

    if (result && result.data) {
      this.updateOwnerRepos(result.data.enabledOwnerRepos);
    }
  }

  updateOwnerRepos(ownerRepos: OwnerRepos[]) {
    // Clean out empty owners.
    const emptyOwners = ownerRepos.filter(o => !o.repos || o.repos.length === 0);
    for (const empty of emptyOwners) {
      const index = ownerRepos.findIndex(o => o.owner === empty.owner);
      ownerRepos.splice(index, 1);
    }

    if (!_.isNil(ownerRepos) && ownerRepos.length === 0) {
      this.updateValueChanged('', '');
    }

    this._ownerReposChangedSubject.next(ownerRepos);
    return this.guardHasRepos(ownerRepos);
  }

  private updateValueChanged(owner: string, repo: string) {
    const isValid = repo && owner && repo.length > 0 && owner.length > 0;
    const ownerRepos = this._ownerReposChangedSubject.value;
    if (isValid) {
      this._valueChangedSubject.next({ owner, repo });
    } else if (this.guardHasRepos(ownerRepos) && this.hasOwnerRepos) {
      const owner = ownerRepos[0].owner;
      const repo = ownerRepos[0].repos[0];
      this._valueChangedSubject.next({ owner, repo });
    }
  }

  private async guardHasRepos(ownerRepos: OwnerRepos[]) {
    // If there are no repos yet, send to that page.
    const path = this._router.url.split('?')[0];
    if (!path || !this._isLoaded) {
      return false;
    }

    let hasRepos = true;
    if ((!ownerRepos || ownerRepos.length === 0) && !path.startsWith('/repositories') && !path.startsWith('/login')) {
      this._router.navigate(['/repositories'], { queryParams: { addrepos: true } });
      hasRepos = false;
    }

    return hasRepos;
  }
}
