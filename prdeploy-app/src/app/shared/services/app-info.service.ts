import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class AppInfoService {
  constructor() {}

  version = '0.0.1';

  public get title() {
    return 'prdeloy';
  }

  public get currentYear() {
    return new Date().getFullYear();
  }
}
