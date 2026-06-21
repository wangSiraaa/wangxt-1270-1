import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, User, UserLoginDto, UserLoginResponse, UserRole } from '../models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.apiBaseUrl}/auth`;
  private currentUserSubject = new BehaviorSubject<UserLoginResponse | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    const saved = localStorage.getItem('current_user');
    if (saved) {
      try {
        this.currentUserSubject.next(JSON.parse(saved));
      } catch {}
    }
  }

  get currentUser(): UserLoginResponse | null {
    return this.currentUserSubject.value;
  }

  get isLoggedIn(): boolean {
    return !!localStorage.getItem('auth_token');
  }

  login(dto: UserLoginDto): Observable<ApiResponse<UserLoginResponse>> {
    return this.http.post<ApiResponse<UserLoginResponse>>(`${this.apiUrl}/login`, dto).pipe(
      tap(res => {
        if (res.success && res.data) {
          localStorage.setItem('auth_token', res.data.token);
          localStorage.setItem('user_role', res.data.role);
          localStorage.setItem('current_user', JSON.stringify(res.data));
          this.currentUserSubject.next(res.data);
        }
      })
    );
  }

  logout(): void {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('user_role');
    localStorage.removeItem('current_user');
    this.currentUserSubject.next(null);
  }

  getCurrentUser(): Observable<ApiResponse<User>> {
    return this.http.get<ApiResponse<User>>(`${this.apiUrl}/me`);
  }

  getUsers(role?: UserRole): Observable<ApiResponse<User[]>> {
    const params: any = {};
    if (role) params.role = role;
    return this.http.get<ApiResponse<User[]>>(`${this.apiUrl}/users`, { params });
  }

  getTeamMembers(): Observable<ApiResponse<User[]>> {
    return this.http.get<ApiResponse<User[]>>(`${this.apiUrl}/team-members`);
  }

  seedDemoData(): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.apiUrl}/seed`, {});
  }

  getHomeRouteByRole(role: UserRole): string {
    switch (role) {
      case 'Agent': return '/agent/policies';
      case 'Supervisor': return '/supervisor/allocation';
      case 'Finance': return '/finance/settlements';
      case 'Admin': return '/finance/settlements';
      default: return '/login';
    }
  }
}
