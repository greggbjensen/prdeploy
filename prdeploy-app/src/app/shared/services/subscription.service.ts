import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, firstValueFrom } from 'rxjs';
import { SignUpModel } from '../models';

@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private baseUrl = 'api/subscription';

  constructor(private http: HttpClient) {}

  public getSignUpModelFromReturnUrl(returnUrl: string): Promise<SignUpModel> {
    return firstValueFrom(
      this.http.post<SignUpModel>(`${this.baseUrl}/getSignUpModelFromReturnUrl`, { returnUrl: returnUrl })
    );
  }

  public getIsFreeTrialAccount(): Observable<boolean> {
    return this.http.get<boolean>(`${this.baseUrl}/isFreeTrial`);
  }

  public getTrialDaysRemaining(): Observable<number> {
    return this.http.get<number>(`${this.baseUrl}/getTrialDaysRemaining`);
  }
}
