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
      return this.validateToken(tokenFromUrl, state.url, tokenFromUrl);
    }

    const currentUser = JSON.parse(localStorage.getItem('currentUser') || '{}');
    if (currentUser?.Token) {
      return this.validateToken(currentUser.Token, state.url, currentUser.Token);
    }

    this.router.navigate(['login'], { queryParams: { returnUrl: state.url } });
    return of(false);
  }

  /** Wait for API validation before allowing the route (fixes refresh kicking to login). */
  private validateToken(token: string, returnUrl: string, tokenToStore: string): Observable<boolean> {
    return this.authService.GetCurrentUser({ Token: token }).pipe(
      map(usr => {
        const user = { ...usr, Token: tokenToStore };
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
