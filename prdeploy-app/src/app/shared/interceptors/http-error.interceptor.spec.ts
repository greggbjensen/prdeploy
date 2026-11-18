import { HttpErrorResponse, HttpHandler, HttpRequest, HttpStatusCode } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { AuthService } from '../services';
import { HttpErrorInterceptor } from './http-error.interceptor';

/** Verifies that the interceptor logs out on auth failures yet always propagates the error. */
describe('HttpErrorInterceptor', () => {
  let authService: jasmine.SpyObj<AuthService>;
  let interceptor: HttpErrorInterceptor;
  const request = new HttpRequest('POST', '/graphql', {});

  beforeEach(() => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['logout']);
    interceptor = new HttpErrorInterceptor(authService);
  });

  /** Creates a handler whose request fails with the given status. */
  function failingHandler(status: number): HttpHandler {
    return { handle: () => throwError(() => new HttpErrorResponse({ status })) };
  }

  it('logs out and re-emits the error on 401', done => {
    interceptor.intercept(request, failingHandler(HttpStatusCode.Unauthorized)).subscribe({
      next: () => fail('should not emit'),
      error: (err: HttpErrorResponse) => {
        expect(err.status).toBe(HttpStatusCode.Unauthorized);
        expect(authService.logout).toHaveBeenCalledWith();
        done();
      },
      complete: () => fail('must not complete without emitting a value')
    });
  });

  it('logs out as forbidden and re-emits the error on 403', done => {
    interceptor.intercept(request, failingHandler(HttpStatusCode.Forbidden)).subscribe({
      error: (err: HttpErrorResponse) => {
        expect(err.status).toBe(HttpStatusCode.Forbidden);
        expect(authService.logout).toHaveBeenCalledWith(true);
        done();
      },
      complete: () => fail('must not complete without emitting a value')
    });
  });

  it('re-emits other errors without logging out', done => {
    interceptor.intercept(request, failingHandler(HttpStatusCode.InternalServerError)).subscribe({
      error: (err: HttpErrorResponse) => {
        expect(err.status).toBe(HttpStatusCode.InternalServerError);
        expect(authService.logout).not.toHaveBeenCalled();
        done();
      }
    });
  });

  it('passes successful responses through', done => {
    const handler: HttpHandler = { handle: () => of({} as never) };
    interceptor.intercept(request, handler).subscribe(value => {
      expect(value).toEqual({} as never);
      expect(authService.logout).not.toHaveBeenCalled();
      done();
    });
  });
});
