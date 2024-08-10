import { Injectable } from '@angular/core';
import { BehaviorSubject, firstValueFrom } from 'rxjs';
import { EnabledOwnerReposGQL, OwnerRepos } from '../graphql';
import _ from 'lodash';

@Injectable({
  providedIn: 'root'
})
export class RepoManager {
  private _repo = '';
  private _owner = '';

  private _ownerReposChangedSubject = new BehaviorSubject<OwnerRepos[]>([]);
  ownerReposChanged$ = this._ownerReposChangedSubject.asObservable();

  private _valueChangedSubject$ = new BehaviorSubject(false);
  valueChanged$ = this._valueChangedSubject$.asObservable();

  get isValid(): boolean {
    return this._valueChangedSubject$.value;
  }

  get repo(): string {
    return this._repo;
  }

  set repo(value: string) {
    if (value !== this.repo) {
      this._repo = value;
      this.updateValueChanged();
    }
  }

  get owner(): string {
    return this._owner;
  }

  set owner(value: string) {
    if (value !== this.owner) {
      this._owner = value;
      this.updateValueChanged();
    }
  }

  constructor(private _enabledOwnerReposGQL: EnabledOwnerReposGQL) {}

  async fetchOwnerRepos() {
    const result = await firstValueFrom(this._enabledOwnerReposGQL.fetch());
    this.updateOwnerRepos(result.data.enabledOwnerRepos);
  }

  updateOwnerRepos(ownerRepos: OwnerRepos[]) {
    // Clean out empty owners.
    const emptyOwners = ownerRepos.filter(o => !o.repos || o.repos.length === 0);
    for (const empty of emptyOwners) {
      const index = ownerRepos.findIndex(o => o.owner === empty.owner);
      ownerRepos.splice(index, 1);
    }

    if (!_.isNil(ownerRepos) && ownerRepos.length === 0) {
      this._valueChangedSubject$.next(true);
    }

    this._ownerReposChangedSubject.next(ownerRepos);
  }

  private updateValueChanged() {
    const isValid = !!this.repo && !!this.owner;
    if (isValid) {
      this._valueChangedSubject$.next(true);
    }
  }
}
