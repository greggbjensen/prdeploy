import { LockoutReason } from './lockout-reason';

export class Lockout {
  isLockedOut: boolean;
  lockoutReason?: LockoutReason;
}
