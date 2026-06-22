import { Component, Input } from '@angular/core';
import { Policy } from '../models';
import { PolicyService } from '../services/policy.service';
import { NzModalService } from 'ng-zorro-antd/modal';

@Component({
  selector: 'app-policy-detail',
  template: `
    <a class="traceable-cell" (click)="showDetail()">查看详情</a>
  `
})
export class PolicyDetailComponent {
  @Input() policy!: Policy;

  constructor(
    private policyService: PolicyService,
    private modal: NzModalService
  ) {}

  showDetail(): void {
    const content = `
      <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
        <div><label>保单号：</label><strong>${this.policy.policyNo}</strong></div>
        <div><label>产品：</label>${this.policy.productName}</div>
        <div><label>投保人：</label>${this.policy.policyHolder}</div>
        <div><label>被保险人：</label>${this.policy.insured}</div>
        <div><label>保费：</label>¥${this.policy.premium.toFixed(2)}</div>
        <div><label>佣金率：</label>${(this.policy.commissionRate * 100).toFixed(2)}%</div>
        <div><label>佣金额：</label><span style="color: #52c41a; font-weight: 600;">¥${this.policy.commissionAmount.toFixed(2)}</span></div>
        <div><label>状态：</label>
          <nz-tag [nzColor]="'${this.policyService.getPolicyStatusColor(this.policy.status)}'">
            ${this.policyService.getPolicyStatusLabel(this.policy.status)}
          </nz-tag>
        </div>
        <div><label>业务员：</label>${this.policy.agentUserName || '-'}</div>
        <div><label>签署日期：</label>${this.policy.signedAt ? this.policy.signedAt.split('T')[0] : '-'}</div>
        <div><label>生效日期：</label>${this.policy.effectiveAt ? this.policy.effectiveAt.split('T')[0] : '-'}</div>
        <div><label>犹豫期截止：</label>${this.policy.coolingPeriodEndAt ? this.policy.coolingPeriodEndAt.split('T')[0] : '-'}</div>
        ${this.policy.cancelledAt ? `
        <div><label>取消日期：</label>${this.policy.cancelledAt.split('T')[0]}</div>
        <div><label>取消类型：</label>${this.policy.cancellationType}</div>
        ` : ''}
      </div>
    `;

    this.modal.create({
      nzTitle: `保单详情 - ${this.policy.policyNo}`,
      nzContent: content,
      nzWidth: 600,
      nzFooter: null
    });
  }
}
