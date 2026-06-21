import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { UserRole } from '../models';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    const token = localStorage.getItem('auth_token');
    if (!token) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
      return false;
    }

    const roles = route.data['roles'] as UserRole[] | undefined;
    if (roles && roles.length > 0) {
      const userRole = localStorage.getItem('user_role') as UserRole;
      if (!roles.includes(userRole)) {
        this.router.navigate(['/403']);
        return false;
      }
    }

    return true;
  }
}
