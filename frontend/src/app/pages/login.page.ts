import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { NzMessageService } from 'ng-zorro-antd/message';

@Component({
  selector: 'app-login',
  template: `
    <div style="min-height: 100vh; display: flex; align-items: center; justify-content: center; background: linear-gradient(135deg, #1890ff 0%, #096dd9 100%);">
      <nz-card style="width: 420px; box-shadow: 0 4px 12px rgba(0,0,0,0.15);">
        <div style="text-align: center; margin-bottom: 24px;">
          <h1 style="font-size: 24px; margin: 0 0 8px; color: #1890ff;">保险经纪佣金结算系统</h1>
          <p style="color: #8c8c8c; margin: 0;">请登录以继续</p>
        </div>
        <form nz-form [formGroup]="loginForm" (ngSubmit)="submit()">
          <nz-form-item>
            <nz-form-control>
              <nz-input-group nzPrefixIcon="user">
                <input formControlName="userCode" nz-input placeholder="账号" autocomplete="username">
              </nz-input-group>
            </nz-form-control>
          </nz-form-item>
          <nz-form-item>
            <nz-form-control>
              <nz-input-group nzPrefixIcon="lock">
                <input formControlName="password" type="password" nz-input placeholder="密码（任意密码即可登录）" autocomplete="current-password">
              </nz-input-group>
            </nz-form-control>
          </nz-form-item>
          <button nz-button nzType="primary" style="width: 100%; height: 40px; font-size: 16px;" [nzLoading]="loading">
            登 录
          </button>
        </form>
        <div style="margin-top: 20px; padding: 12px; background: #f6ffed; border: 1px solid #b7eb8f; border-radius: 4px;">
          <div style="font-size: 12px; color: #52c41a;">
            <strong>演示账号：</strong><br>
            admin / 系统管理员<br>
            finance01 / 财务张经理<br>
            sup01 / 李主管<br>
            agent01 / 业务员小王<br>
            agent02 / 业务员小李
          </div>
        </div>
      </nz-card>
    </div>
  `
})
export class LoginPage implements OnInit {
  loginForm: FormGroup;
  loading = false;
  returnUrl: string;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private msg: NzMessageService
  ) {
    this.loginForm = this.fb.group({
      userCode: ['agent01', Validators.required],
      password: ['123456', Validators.required]
    });
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
  }

  ngOnInit(): void {
    this.authService.seedDemoData().subscribe();
  }

  submit(): void {
    if (this.loginForm.invalid) return;
    this.loading = true;

    this.authService.login(this.loginForm.value).subscribe(res => {
      this.loading = false;
      if (res.success && res.data) {
        this.msg.success(`欢迎, ${res.data.userName}`);
        const target = this.returnUrl === '/' ? this.authService.getHomeRouteByRole(res.data.role) : this.returnUrl;
        this.router.navigateByUrl(target);
      } else {
        this.msg.error(res.message || '登录失败');
      }
    }, () => {
      this.loading = false;
      this.msg.error('登录失败，请检查后端服务是否启动');
    });
  }
}
