import { Component, OnInit } from '@angular/core';
import { PolicyService } from '../../services/policy.service';
import { SettlementService } from '../../services/settlement.service';
import { Policy, MonthlySettlementDto, PolicyQueryDto } from '../../models';

@Component({
  selector: 'app-agent-policies',
  template: `
    <div class="page-wrapper">
      <div class="page-header">
        <h2 class="page-title">我的保单</h2>
      </div>

      <nz-row [nzGutter]="16" style="margin-bottom: 16px;">
        <nz-col [nzSpan]="6">
          <div class="stat-card">
            <div class="stat-value">{{ myPolicies.length }}</div>
            <div class="stat-label">我的保单数</div>
          </div>
        </nz-col>
        <nz-col [nzSpan]="6">
          <div class="stat-card">
            <div class="stat-value" class="money-positive">¥{{ totalCommission.toFixed(2) }}</div>
            <div class="stat-label">累计佣金</div>
          </div>
        </nz-col>
        <nz-col [nzSpan]="6">
          <div class="stat-card">
            <div class="stat-value" class="money-negative">-¥{{ totalClawback.toFixed(2) }}</div>
            <div class="stat-label">累计冲减</div>
          </div>
        </nz-col>
        <nz-col [nzSpan]="6">
          <div class="stat-card">
            <div class="stat-value" class="money-positive">¥{{ totalNet.toFixed(2) }}</div>
            <div class="stat-label">累计实发</div>
          </div>
        </nz-col>
      </nz-row>

      <nz-tabset>
        <nz-tab nzTitle="保单明细">
          <nz-table #policyTable [nzData]="myPolicies" [nzPageSize]="10" nzBordered>
            <thead>
              <tr>
                <th>保单号</th>
                <th>产品</th>
                <th>投保人</th>
                <th nzAlign="right">保费</th>
                <th nzAlign="right">佣金率</th>
                <th nzAlign="right">佣金额</th>
                <th>状态</th>
                <th>生效日</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let p of policyTable.data">
                <td><strong>{{ p.policyNo }}</strong></td>
                <td>{{ p.productName }}</td>
                <td>{{ p.policyHolder }}</td>
                <td nzAlign="right">¥{{ p.premium.toFixed(2) }}</td>
                <td nzAlign="right">{{ (p.commissionRate * 100).toFixed(2) }}%</td>
                <td nzAlign="right" class="money-positive">¥{{ p.commissionAmount.toFixed(2) }}</td>
                <td>
                  <nz-tag [nzColor]="policyService.getPolicyStatusColor(p.status)">
                    {{ policyService.getPolicyStatusLabel(p.status) }}
                  </nz-tag>
                </td>
                <td>{{ p.effectiveAt ? p.effectiveAt.split('T')[0] : '-' }}</td>
                <td>
                  <app-policy-detail [policy]="p"></app-policy-detail>
                </td>
              </tr>
            </tbody>
          </nz-table>
        </nz-tab>

        <nz-tab nzTitle="我的结算">
          <app-settlement-table
            [settlements]="mySettlements"
            [showActions]="false"
          ></app-settlement-table>
        </nz-tab>
      </nz-tabset>
    </div>
  `
})
export class AgentPoliciesPage implements OnInit {
  myPolicies: Policy[] = [];
  mySettlements: MonthlySettlementDto[] = [];

  constructor(
    public policyService: PolicyService,
    private settlementService: SettlementService
  ) {}

  ngOnInit(): void {
    this.loadPolicies();
    this.loadSettlements();
  }

  get totalCommission(): number {
    return this.mySettlements.reduce((s, x) => s + x.totalCommission, 0);
  }

  get totalClawback(): number {
    return this.mySettlements.reduce((s, x) => s + x.totalClawback, 0);
  }

  get totalNet(): number {
    return this.mySettlements.reduce((s, x) => s + x.netPayable, 0);
  }

  private loadPolicies(): void {
    const query: PolicyQueryDto = { pageIndex: 1, pageSize: 100 };
    this.policyService.query(query).subscribe(res => {
      if (res.success && res.data) {
        this.myPolicies = res.data.items;
      }
    });
  }

  private loadSettlements(): void {
    this.settlementService.getMySettlements().subscribe(res => {
      if (res.success && res.data) {
        this.mySettlements = res.data;
      }
    });
  }
}
