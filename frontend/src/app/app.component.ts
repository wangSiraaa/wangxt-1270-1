import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './services/auth.service';
import { UserLoginResponse, UserRole } from './models';

@Component({
  selector: 'app-root',
  template: `
    <ng-container *ngIf="currentUser">
      <nz-layout style="min-height: 100vh;">
        <nz-sider nzTheme="dark" nzWidth="220px">
          <div style="height: 64px; display: flex; align-items: center; justify-content: center; background: #002140;">
            <span style="color: #fff; font-size: 16px; font-weight: 600;">佣金结算系统</span>
          </div>
          <ul nz-menu nzTheme="dark" nzMode="inline" [nzSelectedKey]="selectedKey">
            <ng-container [ngSwitch]="currentUser.role">
              <li nz-menu-item nzMatchRouter nz-match-router-ignore-case (click)="go('/agent/policies')" key="/agent/policies" *ngSwitchCase="'Agent'">
                <i nz-icon nzType="file-text" nzTheme="outline"></i>
                <span>我的保单与结算</span>
              </li>
              <ng-container *ngSwitchCase="'Supervisor'">
                <li nz-menu-item nzMatchRouter (click)="go('/supervisor/allocation')" key="/supervisor/allocation">
                  <i nz-icon nzType="team" nzTheme="outline"></i>
                  <span>团队分摊管理</span>
                </li>
                <li nz-menu-item nzMatchRouter (click)="go('/supervisor/settlements')" key="/supervisor/settlements">
                  <i nz-icon nzType="calculator" nzTheme="outline"></i>
                  <span>团队结算查看</span>
                </li>
              </ng-container>
              <ng-container *ngSwitchDefault>
                <li nz-menu-item nzMatchRouter (click)="go('/finance/settlements')" key="/finance/settlements">
                  <i nz-icon nzType="calculator" nzTheme="outline"></i>
                  <span>佣金结算管理</span>
                </li>
              </ng-container>
            </ng-container>
          </ul>
        </nz-sider>
        <nz-layout>
          <nz-header style="background: #fff; padding: 0 24px; display: flex; justify-content: flex-end; align-items: center; box-shadow: 0 1px 4px rgba(0,21,41,.08);">
            <div>
              <nz-avatar [nzText]="currentUser.userName.charAt(0)" style="background-color: #1890ff; margin-right: 8px;"></nz-avatar>
              <span>{{ currentUser.userName }}</span>
              <nz-tag [nzColor]="roleColor" style="margin-left: 8px;">{{ roleLabel }}</nz-tag>
              <a style="margin-left: 16px;" (click)="logout()">退出登录</a>
            </div>
          </nz-header>
          <nz-content style="background: #f0f2f5;">
            <router-outlet></router-outlet>
          </nz-content>
        </nz-layout>
      </nz-layout>
    </ng-container>
    <router-outlet *ngIf="!currentUser"></router-outlet>
  `
})
export class AppComponent implements OnInit {
  currentUser: UserLoginResponse | null = null;
  selectedKey = '';

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      if (user) {
        this.selectedKey = this.authService.getHomeRouteByRole(user.role);
      }
    });
  }

  get roleLabel(): string {
    const map: Record<UserRole, string> = {
      Agent: '业务员',
      Supervisor: '团队主管',
      Finance: '财务',
      Admin: '管理员'
    };
    return this.currentUser ? map[this.currentUser.role] : '';
  }

  get roleColor(): string {
    const map: Record<UserRole, string> = {
      Agent: 'blue',
      Supervisor: 'cyan',
      Finance: 'green',
      Admin: 'purple'
    };
    return this.currentUser ? map[this.currentUser.role] : 'default';
  }

  go(route: string): void {
    this.selectedKey = route;
    this.router.navigate([route]);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
