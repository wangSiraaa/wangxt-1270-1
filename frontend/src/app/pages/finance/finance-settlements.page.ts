import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { SettlementService } from '../../services/settlement.service';
import { DeductionService } from '../../services/deduction.service';
import { PolicyService } from '../../services/policy.service';
import { AuthService } from '../../services/auth.service';
import {
  MonthlySettlementDto, SettlementQueryDto, PreTaxDeductionDto,
  CreatePreTaxDeductionDto, User, Policy, CreatePolicyDto
} from '../../models';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalService } from 'ng-zorro-antd/modal';

@Component({
  selector: 'app-finance-settlements',
  template: `
    <div class="page-wrapper">
      <div class="page-header">
        <h2 class="page-title">佣金结算管理</h2>
        <div>
          <button nz-button (click)="showDeductionModal()" style="margin-right: 8px;">
            <span nz-icon nzType="minus-circle"></span> 录入税前扣款
          </button>
          <button nz-button (click)="showPolicyModal()" style="margin-right: 8px;">
            <span nz-icon nzType="plus"></span> 录入保单
          </button>
          <button nz-button nzType="primary" (click)="showGenerateModal()">
            <span nz-icon nzType="calculator"></span> 生成月度结算
          </button>
        </div>
      </div>

      <nz-card style="margin-bottom: 16px;">
        <form nz-form [formGroup]="queryForm" (ngSubmit)="doQuery()">
          <nz-row [nzGutter]="16">
            <nz-col [nzSpan]="6">
              <nz-form-item>
                <nz-form-label>结算月份</nz-form-label>
                <nz-form-control>
                  <input type="month" formControlName="settlementMonth" style="width: 100%; padding: 4px 8px; border: 1px solid #d9d9d9; border-radius: 4px;">
                </nz-form-control>
              </nz-form-item>
            </nz-col>
            <nz-col [nzSpan]="6">
              <nz-form-item>
                <nz-form-label>状态</nz-form-label>
                <nz-form-control>
                  <nz-select formControlName="status" nzAllowClear style="width: 100%;">
                    <nz-option nzValue="Draft" nzLabel="草稿"></nz-option>
                    <nz-option nzValue="Generated" nzLabel="已生成"></nz-option>
                    <nz-option nzValue="Approved" nzLabel="已审批"></nz-option>
                    <nz-option nzValue="Paid" nzLabel="已支付"></nz-option>
                    <nz-option nzValue="Rejected" nzLabel="已驳回"></nz-option>
                  </nz-select>
                </nz-form-control>
              </nz-form-item>
            </nz-col>
            <nz-col [nzSpan]="6">
              <nz-form-item>
                <nz-form-label>&nbsp;</nz-form-label>
                <nz-form-control>
                  <button nz-button nzType="primary" (click)="doQuery()">查询</button>
                  <button nz-button style="margin-left: 8px;" (click)="resetQuery()">重置</button>
                </nz-form-control>
              </nz-form-item>
            </nz-col>
          </nz-row>
        </form>
      </nz-card>

      <app-settlement-table
        [settlements]="settlements"
        [showActions]="true"
        [canApprove]="true"
        [canMarkPaid]="true"
      ></app-settlement-table>

      <nz-tabset style="margin-top: 24px;">
        <nz-tab nzTitle="税前扣款记录">
          <nz-table [nzData]="deductions" [nzPageSize]="10" nzBordered>
            <thead>
              <tr>
                <th>月份</th>
                <th>人员</th>
                <th>扣款类型</th>
                <th nzAlign="right">金额</th>
                <th>说明</th>
                <th>录入时间</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let d of deductions">
                <td>{{ d.deductionMonth }}</td>
                <td>{{ d.userName }}</td>
                <td>{{ d.deductionType }}</td>
                <td nzAlign="right" class="money-negative">¥{{ d.deductionAmount.toFixed(2) }}</td>
                <td>{{ d.description || '-' }}</td>
                <td>{{ d.createdAt.split('T')[0] }}</td>
              </tr>
            </tbody>
          </nz-table>
        </nz-tab>

        <nz-tab nzTitle="退保冲减流水">
          <nz-table [nzData]="clawbacks" [nzPageSize]="10" nzBordered>
            <thead>
              <tr>
                <th>保单号</th>
                <th>冲减类型</th>
                <th>是否犹豫期</th>
                <th nzAlign="right">冲减金额</th>
                <th>取消日期</th>
                <th>原因</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let c of clawbacks">
                <td><strong>{{ c.policyNo }}</strong></td>
                <td>{{ c.clawbackType }}</td>
                <td>
                  <nz-tag [nzColor]="c.isCoolingPeriod ? 'orange' : 'default'">
                    {{ c.isCoolingPeriod ? '是' : '否' }}
                  </nz-tag>
                </td>
                <td nzAlign="right" class="money-negative">-¥{{ c.clawbackAmount.toFixed(2) }}</td>
                <td>{{ c.cancelledAt.split('T')[0] }}</td>
                <td>{{ c.reason || '-' }}</td>
              </tr>
            </tbody>
          </nz-table>
        </nz-tab>
      </nz-tabset>
    </div>
  `
})
export class FinanceSettlementsPage implements OnInit {
  queryForm: FormGroup;
  settlements: MonthlySettlementDto[] = [];
  deductions: PreTaxDeductionDto[] = [];
  clawbacks: any[] = [];
  allUsers: User[] = [];
  agents: User[] = [];
  defaultMonth: string;

  constructor(
    private fb: FormBuilder,
    private settlementService: SettlementService,
    private deductionService: DeductionService,
    private policyService: PolicyService,
    private authService: AuthService,
    private msg: NzMessageService,
    private modal: NzModalService
  ) {
    const now = new Date();
    this.defaultMonth = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
    this.queryForm = this.fb.group({
      settlementMonth: [this.defaultMonth],
      status: [null]
    });
  }

  ngOnInit(): void {
    this.loadUsers();
    this.doQuery();
    this.loadDeductions();
    this.loadClawbacks();
  }

  private loadUsers(): void {
    this.authService.getUsers().subscribe(res => {
      if (res.success && res.data) {
        this.allUsers = res.data;
        this.agents = res.data.filter(u => u.role === 'Agent');
      }
    });
  }

  doQuery(): void {
    const query: SettlementQueryDto = {
      pageIndex: 1,
      pageSize: 100,
      settlementMonth: this.queryForm.get('settlementMonth')?.value || undefined,
      status: this.queryForm.get('status')?.value || undefined
    };
    this.settlementService.query(query).subscribe(res => {
      if (res.success && res.data) {
        this.settlements = res.data.items;
      }
    });
  }

  resetQuery(): void {
    this.queryForm.reset({ settlementMonth: this.defaultMonth, status: null });
    this.doQuery();
  }

  private loadDeductions(): void {
    this.deductionService.getByMonth(this.defaultMonth).subscribe(res => {
      if (res.success && res.data) {
        this.deductions = res.data;
      }
    });
  }

  private loadClawbacks(): void {
    this.settlementService.getClawbacks().subscribe(res => {
      if (res.success && res.data) {
        this.clawbacks = res.data;
      }
    });
  }

  showGenerateModal(): void {
    const tpl = `
      <div style="padding: 8px 0;">
        <label><strong>结算月份 *</strong></label>
        <input type="month" #monthInput value="${this.defaultMonth}"
          style="width: 100%; padding: 4px 8px; border: 1px solid #d9d9d9; border-radius: 4px; margin-top: 4px;">
        <div style="color: #8c8c8c; font-size: 12px; margin-top: 8px;">
          <p>生成规则：</p>
          <ul>
            <li>仅处理已生效保单的佣金</li>
            <li>犹豫期内退保将全额冲回佣金</li>
            <li>自动应用当月的分摊比例</li>
            <li>扣除当月录入的税前扣款</li>
          </ul>
        </div>
      </div>
    `;
    this.modal.create({
      nzTitle: '生成月度佣金结算单',
      nzContent: tpl,
      nzWidth: 500,
      nzOnOk: (comp: any) => {
        const month = comp.monthInput?.nativeElement?.value || this.defaultMonth;
        return this.settlementService.generate({ settlementMonth: month }).subscribe(res => {
          if (res.success && res.data) {
            this.msg.success(`成功生成 ${res.data.length} 条结算单`);
            this.doQuery();
          } else {
            this.msg.error(res.message);
          }
        });
      }
    });
  }

  showDeductionModal(): void {
    const tpl = `
      <div style="padding: 8px 0;">
        <div style="margin-bottom: 12px;">
          <label><strong>人员 *</strong></label>
          <nz-select #userSel style="width: 100%;">
            <nz-option *ngFor="let u of allUsers" [nzValue]="u.userId" [nzLabel]="u.userName + ' (' + u.userCode + ')'"></nz-option>
          </nz-select>
        </div>
        <div style="margin-bottom: 12px;">
          <label><strong>扣款类型 *</strong></label>
          <input #typeInput placeholder="如：社保、公积金、罚款等"
            style="width: 100%; padding: 4px 8px; border: 1px solid #d9d9d9; border-radius: 4px;">
        </div>
        <div style="margin-bottom: 12px;">
          <label><strong>金额 *</strong></label>
          <nz-input-number #amountInput [nzMin]="0" [nzStep]="100" style="width: 100%;"></nz-input-number>
        </div>
        <div style="margin-bottom: 12px;">
          <label><strong>所属月份 *</strong></label>
          <input type="month" #monthInput value="${this.defaultMonth}"
            style="width: 100%; padding: 4px 8px; border: 1px solid #d9d9d9; border-radius: 4px;">
        </div>
        <div>
          <label>说明</label>
          <textarea #descInput rows="2" placeholder="选填"
            style="width: 100%; padding: 4px 8px; border: 1px solid #d9d9d9; border-radius: 4px;"></textarea>
        </div>
      </div>
    `;
    this.modal.create({
      nzTitle: '录入税前扣款',
      nzContent: tpl,
      nzWidth: 500,
      nzOnOk: (comp: any) => {
        const dto: CreatePreTaxDeductionDto = {
          userId: comp.userSel?.value || this.agents[0]?.userId,
          deductionType: comp.typeInput?.nativeElement?.value || '',
          deductionAmount: comp.amountInput?.value || 0,
          deductionMonth: comp.monthInput?.nativeElement?.value || this.defaultMonth,
          description: comp.descInput?.nativeElement?.value || null
        };
        return this.deductionService.create(dto).subscribe(res => {
          if (res.success) {
            this.msg.success('扣款已录入');
            this.loadDeductions();
          } else {
            this.msg.error(res.message);
          }
        });
      }
    });
  }

  showPolicyModal(): void {
    const tpl = `
      <div style="padding: 8px 0;">
        <div style="margin-bottom: 12px;">
          <label><strong>保单号 *</strong></label>
          <input #noInput placeholder="如：POL20250001"
            style="width: 100%; padding: 4px 8px; border: 1px solid #d9d9d9; border-radius: 4px;">
        </div>
        <div style="margin-bottom: 12px;">
          <label><strong>产品名称 *</strong></label>
          <input #prodInput placeholder="如：重大疾病保险"
            style="width: 100%; padding: 4px 8px; border: 1px solid #d9d9d9; border-radius: 4px;">
        </div>
        <div style="margin-bottom: 12px;">
          <label><strong>投保人 *</strong></label>
          <input #holderInput placeholder="姓名"
            style="width: 100%; padding: 4px 8px; border: 1px solid #d9d9d9; border-radius: 4px;">
        </div>
        <div style="margin-bottom: 12px;">
          <label><strong>被保险人 *</strong></label>
          <input #insuredInput placeholder="姓名"
            style="width: 100%; padding: 4px 8px; border: 1px solid #d9d9d9; border-radius: 4px;">
        </div>
        <div style="margin-bottom: 12px;">
          <label><strong>业务员 *</strong></label>
          <nz-select #agentSel style="width: 100%;">
            <nz-option *ngFor="let a of agents" [nzValue]="a.userId" [nzLabel]="a.userName + ' (' + a.userCode + ')'"></nz-option>
          </nz-select>
        </div>
        <div style="margin-bottom: 12px;">
          <label><strong>保费 *</strong></label>
          <nz-input-number #premInput [nzMin]="0" [nzStep]="1000" style="width: 100%;"></nz-input-number>
        </div>
        <div>
          <label><strong>佣金率 (%)</strong> <span style="color:#8c8c8c;">(默认 20%)</span></label>
          <nz-input-number #rateInput [nzMin]="0" [nzMax]="1" [nzStep]="0.01" style="width: 100%;"></nz-input-number>
        </div>
      </div>
    `;
    this.modal.create({
      nzTitle: '录入保单',
      nzContent: tpl,
      nzWidth: 500,
      nzOnOk: (comp: any) => {
        const dto: CreatePolicyDto = {
          policyNo: comp.noInput?.nativeElement?.value || '',
          productName: comp.prodInput?.nativeElement?.value || '',
          policyHolder: comp.holderInput?.nativeElement?.value || '',
          insured: comp.insuredInput?.nativeElement?.value || '',
          premium: comp.premInput?.value || 0,
          commissionRate: comp.rateInput?.value || 0.20,
          agentUserId: comp.agentSel?.value || this.agents[0]?.userId
        };
        return this.policyService.create(dto).subscribe(res => {
          if (res.success) {
            this.msg.success('保单录入成功');
          } else {
            this.msg.error(res.message);
          }
        });
      }
    });
  }
}
