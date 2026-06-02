import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpErrorResponse,
  HttpClient
} from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { Configuration } from '../app.constants';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private refreshInProgress = false;
  private refreshDone$ = new BehaviorSubject<boolean>(false);

  constructor(private http: HttpClient, private config: Configuration) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const rememberMe = this.isRememberMeUser();
    const authReq = rememberMe
      ? req.clone({ withCredentials: true })
      : req;

    return next.handle(authReq).pipe(
      catchError((err: HttpErrorResponse) => {
        if (!rememberMe || err.status !== 401) {
          return throwError(() => err);
        }
        if (this.isAuthEndpoint(req.url)) {
          return throwError(() => err);
        }
        return this.tryRefreshAndRetry(authReq, next);
      })
    );
  }

  private tryRefreshAndRetry(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (!this.refreshInProgress) {
      this.refreshInProgress = true;
      this.refreshDone$.next(false);

      return this.http.post<any>(`${this.config.WebApi}/User/refresh-token`, {}, { withCredentials: true }).pipe(
        switchMap((res) => {
          this.refreshInProgress = false;
          this.refreshDone$.next(true);

          let newReq = req;
          if (res && res.token) {
            try {
              const u = JSON.parse(localStorage.getItem('currentUser') || '{}');
              u.Token = res.token;
              localStorage.setItem('currentUser', JSON.stringify(u));
              newReq = req.clone({
                headers: req.headers.set('Authorization', `Bearer ${res.token}`)
              });
            } catch (e) {}
          }

          const isRemUser = this.isRememberMeUser();
          if (isRemUser) {
            newReq = newReq.clone({ withCredentials: true });
          }

          return next.handle(newReq);
        }),
        catchError(refreshErr => {
          this.refreshInProgress = false;
          localStorage.removeItem('currentUser');
          return throwError(() => refreshErr);
        })
      );
    }

    return this.refreshDone$.pipe(
      filter(done => done),
      take(1),
      switchMap(() => {
        let newReq = req;
        try {
          const u = JSON.parse(localStorage.getItem('currentUser') || '{}');
          if (u && u.Token) {
            newReq = req.clone({
              headers: req.headers.set('Authorization', `Bearer ${u.Token}`)
            });
          }
        } catch (e) {}

        if (this.isRememberMeUser()) {
          newReq = newReq.clone({ withCredentials: true });
        }

        return next.handle(newReq);
      })
    );
  }

  private isRememberMeUser(): boolean {
    try {
      const u = JSON.parse(localStorage.getItem('currentUser') || '{}');
      return u?.RememberUser === true || u?.RememberUser === 'true' || u?.RememberUser === '1' || u?.RememberUser === 1;
    } catch {
      return false;
    }
  }

  private isAuthEndpoint(url: string): boolean {
    return url.includes('/User/authenticate') || url.includes('/User/refresh-token');
  }
}
