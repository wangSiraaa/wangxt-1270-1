import { Component, OnInit } from '@angular/core';
import { PolicyService } from '../../services/policy.service';
import { AllocationService } from '../../services/allocation.service';
import { AuthService } from '../../services/auth.service';
import { Policy, AllocationAdjustmentDto, PolicyQueryDto, User } from '../../models';

@Component({
  selector: 'app-supervisor-allocation',
  template: `
    <div class="page-wrapper">
      <div class="page-header">
        <h2 class="page-title">团队分摊比例管理</h2>
      </div>

      <div style="margin-bottom: 16px; padding: 12px; background: #e6f7ff; border: 1px solid #91d5ff; border-radius: 4px;">
        <span nz-icon nzType="info-circle" nzTheme="fill" style="color: #1890ff;"></span>
        <strong>提示：</strong>调整分摊比例将从指定日期起生效，<span style="color: #ff4d4f;">不会修改已生成的历史结算数据</span>。每次调整必须填写原因。
      </div>

      <nz-table #teamTable [nzData]="teamPolicies" [nzPageSize]="10" nzBordered>
        <thead>
          <tr>
            <th>保单号</th>
            <th>产品</th>
            <th>业务员</th>
            <th nzAlign="right">保费</th>
            <th nzAlign="right">佣金额</th>
            <th>状态</th>
            <th>当前分摊方案</th>
            <th nzWidth="180px">操作</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let p of teamTable.data">
            <td><strong>{{ p.policyNo }}</strong></td>
            <td>{{ p.productName }}</td>
            <td>{{ p.agentUserName }}</td>
            <td nzAlign="right">¥{{ p.premium.toFixed(2) }}</td>
            <td nzAlign="right" class="money-positive">¥{{ p.commissionAmount.toFixed(2) }}</td>
            <td>
              <nz-tag [nzColor]="policyService.getPolicyStatusColor(p.status)">
                {{ policyService.getPolicyStatusLabel(p.status) }}
              </nz-tag>
            </td>
            <td>
              <button nz-button nzType="text" (click)="viewAllocation(p)">查看分摊</button>
              <button nz-button nzType="text" (click)="viewHistory(p)">调整历史</button>
            </td>
            <td>
              <app-allocation-adjust [policy]="p" [showAsButton]="true"></app-allocation-adjust>
            </td>
          </tr>
        </tbody>
      </nz-table>
    </div>
  `
})
export class SupervisorAllocationPage implements OnInit {
  teamPolicies: Policy[] = [];
  teamMembers: User[] = [];

  constructor(
    public policyService: PolicyService,
    private allocationService: AllocationService,
    private authService: AuthService,
    private modal: any
  ) {}

  ngOnInit(): void {
    this.authService.getTeamMembers().subscribe(res => {
      if (res.success && res.data) {
        this.teamMembers = res.data;
        this.loadTeamPolicies();
      }
    });
  }

  private loadTeamPolicies(): void {
    const ids = this.teamMembers.map(m => m.userId);
    const query: PolicyQueryDto = { pageIndex: 1, pageSize: 100 };
    this.policyService.query(query).subscribe(res => {
      if (res.success && res.data) {
        this.teamPolicies = res.data.items.filter(p => ids.includes(p.agentUserId));
      }
    });
  }

  viewAllocation(policy: Policy): void {
    this.allocationService.getActiveRule(policy.policyId).subscribe(res => {
      if (res.success && res.data) {
        const rule = res.data;
        const content = `
          <div>
            <p><strong>保单号：</strong>${policy.policyNo}</p>
            <p><strong>生效日期：</strong>${rule.effectiveStartDate.split('T')[0]}</p>
            <table style="width: 100%; border-collapse: collapse;">
              <thead><tr style="background:#f5f5f5;">
                <th style="padding:8px; border:1px solid #e8e8e8;">人员</th>
                <th style="padding:8px; border:1px solid #e8e8e8;">角色</th>
                <th style="padding:8px; border:1px solid #e8e8e8;">比例</th>
                <th style="padding:8px; border:1px solid #e8e8e8;">金额</th>
              </tr></thead>
              <tbody>
                ${rule.details.map(d => `
                  <tr>
                    <td style="padding:8px; border:1px solid #e8e8e8;">${d.userName}</td>
                    <td style="padding:8px; border:1px solid #e8e8e8;">${d.roleType}</td>
                    <td style="padding:8px; border:1px solid #e8e8e8;">${(d.allocationRatio * 100).toFixed(2)}%</td>
                    <td style="padding:8px; border:1px solid #e8e8e8;">¥${d.allocatedAmount.toFixed(2)}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        `;
        // 使用全局注入
      }
    });
  }

  viewHistory(policy: Policy): void {
    this.allocationService.getAdjustmentHistory(policy.policyId).subscribe(res => {
      if (res.success && res.data) {
        const history: AllocationAdjustmentDto[] = res.data;
        const content = `
          <div style="max-height: 500px; overflow: auto;">
            ${history.length === 0 ? '<p style="color:#8c8c8c;">暂无调整历史</p>' : history.map(h => `
              <div style="border: 1px solid #e8e8e8; border-radius: 4px; padding: 12px; margin-bottom: 12px;">
                <div style="display: flex; justify-content: space-between; margin-bottom: 8px;">
                  <strong>生效日：${h.effectiveFromDate.split('T')[0]}</strong>
                  <span style="color:#8c8c8c; font-size:12px;">${h.createdAt.split('T')[0]}</span>
                </div>
                <div style="background: #fff2e8; padding: 8px; border-radius: 4px; margin-bottom: 8px;">
                  <strong>调整原因：</strong>${h.adjustmentReason}
                </div>
                ${h.oldDetails ? `
                <div style="margin-bottom: 8px;">
                  <label style="color:#ff4d4f;">调整前：</label>
                  ${h.oldDetails.map(d => `${d.userName}(${(d.allocationRatio * 100).toFixed(2)}%)`).join(' / ')}
                </div>` : ''}
                <div>
                  <label style="color:#52c41a;">调整后：</label>
                  ${h.newDetails.map(d => `${d.userName}(${(d.allocationRatio * 100).toFixed(2)}%)`).join(' / ')}
                </div>
              </div>
            `).join('')}
          </div>
        `;
      }
    });
  }
}
