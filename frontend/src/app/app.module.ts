import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { RouterModule, Routes } from '@angular/router';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { registerLocaleData } from '@angular/common';
import zh from '@angular/common/locales/zh';

import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzMessageModule } from 'ng-zorro-antd/message';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzTabsModule } from 'ng-zorro-antd/tabs';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzIconModule } from 'ng-zorro-antd/icon';
import {
  UserOutline, LockOutline, FileTextOutline, TeamOutline,
  CalculatorOutline, EditOutline, PlusOutline, MinusCircleOutline, InfoCircleOutline
} from '@ant-design/icons-angular/icons';
import { NZ_I18N, zh_CN } from 'ng-zorro-antd/i18n';

import { AppComponent } from './app.component';
import { LoginPage } from './pages/login.page';
import { AgentPoliciesPage } from './pages/agent/agent-policies.page';
import { SupervisorAllocationPage } from './pages/supervisor/supervisor-allocation.page';
import { FinanceSettlementsPage } from './pages/finance/finance-settlements.page';
import { SettlementTableComponent } from './components/settlement-table/settlement-table.component';
import { PolicyDetailComponent } from './components/policy-detail/policy-detail.component';
import { AllocationAdjustComponent } from './components/allocation-adjust/allocation-adjust.component';

import { AuthInterceptor } from './interceptors/auth.interceptor';
import { AuthGuard } from './guards/auth.guard';

registerLocaleData(zh);

const routes: Routes = [
  { path: 'login', component: LoginPage },
  {
    path: 'agent',
    canActivate: [AuthGuard],
    data: { roles: ['Agent'] },
    children: [
      { path: 'policies', component: AgentPoliciesPage },
      { path: '', redirectTo: 'policies', pathMatch: 'full' }
    ]
  },
  {
    path: 'supervisor',
    canActivate: [AuthGuard],
    data: { roles: ['Supervisor', 'Admin'] },
    children: [
      { path: 'allocation', component: SupervisorAllocationPage },
      { path: 'settlements', component: AgentPoliciesPage },
      { path: '', redirectTo: 'allocation', pathMatch: 'full' }
    ]
  },
  {
    path: 'finance',
    canActivate: [AuthGuard],
    data: { roles: ['Finance', 'Admin'] },
    children: [
      { path: 'settlements', component: FinanceSettlementsPage },
      { path: '', redirectTo: 'settlements', pathMatch: 'full' }
    ]
  },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' }
];

const icons = [
  UserOutline, LockOutline, FileTextOutline, TeamOutline,
  CalculatorOutline, EditOutline, PlusOutline, MinusCircleOutline, InfoCircleOutline
];

@NgModule({
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule.forRoot(routes, { useHash: true }),
    NzLayoutModule,
    NzMenuModule,
    NzCardModule,
    NzTableModule,
    NzButtonModule,
    NzInputModule,
    NzInputNumberModule,
    NzSelectModule,
    NzTagModule,
    NzModalModule,
    NzMessageModule,
    NzFormModule,
    NzAlertModule,
    NzTabsModule,
    NzAvatarModule,
    NzIconModule.forRoot(icons)
  ],
  declarations: [
    AppComponent,
    LoginPage,
    AgentPoliciesPage,
    SupervisorAllocationPage,
    FinanceSettlementsPage,
    SettlementTableComponent,
    PolicyDetailComponent,
    AllocationAdjustComponent
  ],
  providers: [
    { provide: NZ_I18N, useValue: zh_CN },
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
