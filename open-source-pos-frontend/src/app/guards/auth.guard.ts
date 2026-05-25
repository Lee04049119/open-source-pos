import { Injectable } from '@angular/core';
import { Router, CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { AuthService } from '../views/login/auth.service';

@Injectable()
export class AuthGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) { }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> {
    const tokenFromUrl = route.queryParams['Q'];

    if (tokenFromUrl) {
      return this.validateSession({ Token: tokenFromUrl }, state.url);
    }

    const currentUser = JSON.parse(localStorage.getItem('currentUser') || '{}');
    if (currentUser?.RememberUser && currentUser?.SessionToken) {
      return this.validateSession(currentUser, state.url);
    }
    if (currentUser?.Token) {
      return this.validateSession({ ...currentUser, Token: currentUser.Token }, state.url);
    }

    this.router.navigate(['login'], { queryParams: { returnUrl: state.url } });
    return of(false);
  }

  /** Wait for API validation before allowing the route (fixes refresh kicking to login). */
  private validateSession(profile: any, returnUrl: string): Observable<boolean> {
    return this.authService.GetCurrentUser(profile).pipe(
      map(usr => {
        const user = { ...profile, ...usr };
        if (profile.Token) {
          user.Token = profile.Token;
        }
        localStorage.setItem('currentUser', JSON.stringify(user));
        return true;
      }),
      catchError(() => {
        localStorage.removeItem('currentUser');
        this.router.navigate(['login'], { queryParams: { returnUrl } });
        return of(false);
      })
    );
  }
}
