import { Component, Input } from '@angular/core';
import { MonthlySettlementDto, SettlementSnapshotDto } from '../../models';
import { SettlementService } from '../../services/settlement.service';
import { NzModalService } from 'ng-zorro-antd/modal';
import { NzMessageService } from 'ng-zorro-antd/message';

@Component({
  selector: 'app-settlement-table',
  template: `
    <div class="settlement-wrapper">
      <nz-table
        #settlementTable
        [nzData]="settlements"
        [nzPageSize]="10"
        [nzShowPagination]="showPagination"
        nzBordered
      >
        <thead>
          <tr>
            <th>结算月份</th>
            <th>业务员</th>
            <th nzAlign="right">佣金总额</th>
            <th nzAlign="right">退保冲减</th>
            <th nzAlign="right">税前扣款</th>
            <th nzAlign="right">应纳税所得</th>
            <th nzAlign="right">个人所得税</th>
            <th nzAlign="right" class="net-payable">实发金额</th>
            <th>状态</th>
            <th nzAlign="center" nzWidth="200px">操作</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let s of settlementTable.data">
            <td>
              <a class="traceable-cell" (click)="viewDetail(s)">{{ s.settlementMonth }}</a>
            </td>
            <td>{{ s.userName }} ({{ s.userCode }})</td>
            <td nzAlign="right" class="money-positive">¥{{ s.totalCommission.toFixed(2) }}</td>
            <td nzAlign="right" class="money-negative">-¥{{ s.totalClawback.toFixed(2) }}</td>
            <td nzAlign="right" class="money-negative">-¥{{ s.totalPreTaxDeduction.toFixed(2) }}</td>
            <td nzAlign="right">¥{{ s.taxableIncome.toFixed(2) }}</td>
            <td nzAlign="right" class="money-negative">-¥{{ s.incomeTax.toFixed(2) }}</td>
            <td nzAlign="right" class="net-payable money-positive">
              <strong>¥{{ s.netPayable.toFixed(2) }}</strong>
            </td>
            <td>
              <nz-tag [nzColor]="getStatusColor(s.status)">{{ getStatusLabel(s.status) }}</nz-tag>
            </td>
            <td nzAlign="center">
              <button nz-button nzType="link" (click)="viewDetail(s)">追溯明细</button>
              <ng-container *ngIf="showActions">
                <button
                  *ngIf="s.status === 'Generated' && canApprove"
                  nz-button nzType="link"
                  nzDanger
                  (click)="handleReject(s)"
                >驳回</button>
                <button
                  *ngIf="s.status === 'Generated' && canApprove"
                  nz-button nzType="link"
                  (click)="handleApprove(s)"
                >审批</button>
                <button
                  *ngIf="s.status === 'Approved' && canMarkPaid"
                  nz-button nzType="link"
                  (click)="handleMarkPaid(s)"
                >标记已付</button>
              </ng-container>
            </td>
          </tr>
        </tbody>
      </nz-table>
    </div>
  `,
  styles: [`
    .net-payable { font-size: 16px; }
    .traceable-cell {
      cursor: pointer;
      color: #1890ff;
      text-decoration: underline;
    }
  `]
})
export class SettlementTableComponent {
  @Input() settlements: MonthlySettlementDto[] = [];
  @Input() showPagination = true;
  @Input() showActions = false;
  @Input() canApprove = false;
  @Input() canMarkPaid = false;

  constructor(
    private settlementService: SettlementService,
    private modal: NzModalService,
    private msg: NzMessageService
  ) {}

  getStatusLabel = (status: string) => this.settlementService.getStatusLabel(status as any);
  getStatusColor = (status: string) => this.settlementService.getStatusColor(status as any);

  viewDetail(settlement: MonthlySettlementDto): void {
    this.settlementService.getSnapshots(settlement.settlementId).subscribe(res => {
      if (res.success && res.data) {
        this.showSettlementDetail(settlement, res.data);
      }
    });
  }

  private showSettlementDetail(settlement: MonthlySettlementDto, snapshots: SettlementSnapshotDto[]): void {
    const content = `
      <div style="max-height: 500px; overflow: auto;">
        <div style="margin-bottom: 16px; padding: 12px; background: #fafafa; border-radius: 4px;">
          <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px;">
            <div><strong>结算月份：</strong>${settlement.settlementMonth}</div>
            <div><strong>业务员：</strong>${settlement.userName}</div>
            <div><strong>佣金总额：</strong>¥${settlement.totalCommission.toFixed(2)}</div>
            <div><strong>退保冲减：</strong>¥${settlement.totalClawback.toFixed(2)}</div>
            <div><strong>税前扣款：</strong>¥${settlement.totalPreTaxDeduction.toFixed(2)}</div>
            <div><strong>应纳税所得：</strong>¥${settlement.taxableIncome.toFixed(2)}</div>
            <div><strong>个人所得税：</strong>¥${settlement.incomeTax.toFixed(2)}</div>
            <div style="color: #52c41a; font-weight: 600;"><strong>实发金额：</strong>¥${settlement.netPayable.toFixed(2)}</div>
          </div>
        </div>
        <h4>保单明细（共 ${snapshots.length} 条）</h4>
        <table style="width: 100%; border-collapse: collapse; font-size: 12px;">
          <thead>
            <tr style="background: #f5f5f5;">
              <th style="padding: 8px; border: 1px solid #e8e8e8;">保单号</th>
              <th style="padding: 8px; border: 1px solid #e8e8e8;">保费</th>
              <th style="padding: 8px; border: 1px solid #e8e8e8;">佣金率</th>
              <th style="padding: 8px; border: 1px solid #e8e8e8;">原佣金额</th>
              <th style="padding: 8px; border: 1px solid #e8e8e8;">分摊比例</th>
              <th style="padding: 8px; border: 1px solid #e8e8e8;">分摊佣金</th>
              <th style="padding: 8px; border: 1px solid #e8e8e8;">冲减</th>
            </tr>
          </thead>
          <tbody>
            ${snapshots.map(s => `
              <tr>
                <td style="padding: 8px; border: 1px solid #e8e8e8;">
                  <span style="color: #1890ff;">${s.policyNo}</span>
                  <div style="font-size: 11px; color: #8c8c8c;">状态: ${s.policyStatus}</div>
                </td>
                <td style="padding: 8px; border: 1px solid #e8e8e8;">¥${s.premium.toFixed(2)}</td>
                <td style="padding: 8px; border: 1px solid #e8e8e8;">${(s.commissionRate * 100).toFixed(2)}%</td>
                <td style="padding: 8px; border: 1px solid #e8e8e8;">¥${s.originalCommission.toFixed(2)}</td>
                <td style="padding: 8px; border: 1px solid #e8e8e8;">${(s.allocationRatio * 100).toFixed(2)}%</td>
                <td style="padding: 8px; border: 1px solid #e8e8e8; color: #52c41a;">¥${s.allocatedCommission.toFixed(2)}</td>
                <td style="padding: 8px; border: 1px solid #e8e8e8; color: #ff4d4f;">${s.clawbackAmount > 0 ? '-¥' + s.clawbackAmount.toFixed(2) : '-'}</td>
              </tr>
              ${s.ruleSnapshot ? `
              <tr>
                <td colspan="7" style="padding: 4px 8px; border: 1px solid #e8e8e8; background: #fafbfc; font-size: 11px; color: #595959;">
                  <strong>规则快照：</strong>${this.formatRuleSnapshot(s.ruleSnapshot)}
                </td>
              </tr>` : ''}
            `).join('')}
          </tbody>
        </table>
      </div>
    `;

    this.modal.create({
      nzTitle: `结算明细追溯 - ${settlement.settlementMonth}`,
      nzContent: content,
      nzWidth: 900,
      nzFooter: null
    });
  }

  private formatRuleSnapshot(json: string): string {
    try {
      const obj = JSON.parse(json);
      return `规则ID: ${obj.RuleId?.substring(0, 8)}..., 生效日: ${obj.EffectiveStartDate?.split('T')[0] || obj.EffectiveStartDate}, 角色: ${obj.RoleType}`;
    } catch {
      return json;
    }
  }

  handleApprove(settlement: MonthlySettlementDto): void {
    this.modal.confirm({
      nzTitle: '确认审批通过?',
      nzContent: `结算月份: ${settlement.settlementMonth}, 业务员: ${settlement.userName}, 实发金额: ¥${settlement.netPayable.toFixed(2)}`,
      nzOkText: '确认审批',
      nzCancelText: '取消',
      nzOnOk: () => this.settlementService.approve(settlement.settlementId).subscribe(res => {
        if (res.success) {
          this.msg.success('审批通过');
          Object.assign(settlement, res.data);
        } else {
          this.msg.error(res.message);
        }
      })
    });
  }

  handleReject(settlement: MonthlySettlementDto): void {
    this.modal.create({
      nzTitle: '驳回结算单',
      nzContent: '<textarea nz-input rows="3" placeholder="请输入驳回原因" #reasonInput></textarea>',
      nzOnOk: (component: any) => {
        const reason = component.reasonInput?.nativeElement?.value || '无原因';
        return this.settlementService.reject(settlement.settlementId, reason).subscribe(res => {
          if (res.success) {
            this.msg.success('已驳回');
            Object.assign(settlement, res.data);
          } else {
            this.msg.error(res.message);
          }
        });
      }
    });
  }

  handleMarkPaid(settlement: MonthlySettlementDto): void {
    this.modal.confirm({
      nzTitle: '确认标记已支付?',
      nzContent: `确认已向 ${settlement.userName} 支付 ¥${settlement.netPayable.toFixed(2)}?`,
      nzOnOk: () => this.settlementService.markPaid(settlement.settlementId).subscribe(res => {
        if (res.success) {
          this.msg.success('已标记支付');
          Object.assign(settlement, res.data);
        } else {
          this.msg.error(res.message);
        }
      })
    });
  }
}
