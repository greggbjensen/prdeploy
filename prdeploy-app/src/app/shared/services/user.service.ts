import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { User } from '../models';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  set user(value: User) {
    this._user = value;
    this._userSubject.next(value);
  }

  get user(): User {
    return this._user;
  }

  $user: Observable<User>;
  private _user: User = null;
  private _userSubject = new BehaviorSubject<User>(null);

  constructor() {
    this.$user = this._userSubject.asObservable();
  }
}
