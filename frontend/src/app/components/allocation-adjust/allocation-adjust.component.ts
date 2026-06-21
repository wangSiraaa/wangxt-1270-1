import { Component, Input, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { Policy, User, AdjustAllocationDto, AllocationRuleDto, CreateAllocationRuleDetailDto } from '../../models';
import { AllocationService } from '../../services/allocation.service';
import { AuthService } from '../../services/auth.service';
import { NzModalService } from 'ng-zorro-antd/modal';
import { NzMessageService } from 'ng-zorro-antd/message';

@Component({
  selector: 'app-allocation-adjust',
  template: `
    <button *ngIf="showAsButton" nz-button nzType="primary" (click)="openModal()">
      <span nz-icon nzType="edit"></span> 调整分摊比例
    </button>
    <ng-container *ngIf="!showAsButton">
      <a class="traceable-cell" (click)="openModal()">调整分摊</a>
    </ng-container>
  `
})
export class AllocationAdjustComponent implements OnInit {
  @Input() policy!: Policy;
  @Input() showAsButton = true;
  teamMembers: User[] = [];
  currentRule: AllocationRuleDto | null = null;
  form!: FormGroup;

  constructor(
    private fb: FormBuilder,
    private allocationService: AllocationService,
    private authService: AuthService,
    private modal: NzModalService,
    private msg: NzMessageService
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      adjustmentReason: ['', [Validators.required, Validators.maxLength(500)]],
      effectiveFromDate: [new Date().toISOString().split('T')[0], Validators.required],
      details: this.fb.array([])
    });
  }

  get details(): FormArray {
    return this.form.get('details') as FormArray;
  }

  openModal(): void {
    this.authService.getTeamMembers().subscribe(res => {
      if (res.success && res.data) {
        this.teamMembers = res.data;
      }
    });

    this.allocationService.getActiveRule(this.policy.policyId).subscribe(res => {
      if (res.success && res.data) {
        this.currentRule = res.data;
        this.loadCurrentRule(res.data);
      } else {
        this.addDetailRow();
      }
    });

    this.modal.create({
      nzTitle: `调整保单分摊比例 - ${this.policy.policyNo}`,
      nzWidth: 650,
      nzContent: this.buildModalContent(),
      nzOkText: '确认调整',
      nzCancelText: '取消',
      nzOkDisabled: !this.form.valid || this.getTotalRatio() !== 1,
      nzOnOk: () => this.submitAdjustment()
    });
  }

  private buildModalContent(): string {
    return `
      <div style="margin-bottom: 16px;">
        <nz-alert nzType="warning" nzShowIcon
          nzMessage="重要说明"
          nzDescription="分摊比例调整将从指定日期生效，不会修改已生成的历史结算单。所有比例之和必须等于 100%。">
        </nz-alert>
      </div>
      <div style="margin-bottom: 12px;">
        <label><strong>调整原因 *</strong></label>
        <textarea nz-input rows="2" placeholder="请填写调整原因（必填，用于追溯审计）"
          [formControl]="form.get('adjustmentReason')"
          style="width: 100%;"></textarea>
      </div>
      <div style="margin-bottom: 16px;">
        <label><strong>生效日期 *</strong></label>
        <input type="date" [formControl]="form.get('effectiveFromDate')"
          style="width: 100%; padding: 4px 8px; border: 1px solid #d9d9d9; border-radius: 4px;">
        <div style="color: #8c8c8c; font-size: 12px; margin-top: 4px;">生效日期不能早于今天，禁止改写历史</div>
      </div>
      <div style="margin-bottom: 8px;">
        <label><strong>分摊明细</strong>
          <span [style.color]="this.getTotalRatio() === 1 ? '#52c41a' : '#ff4d4f'">
            （合计: {{ (this.getTotalRatio() * 100).toFixed(2) }}%）
          </span>
        </label>
      </div>
      <table style="width: 100%; border-collapse: collapse;">
        <thead>
          <tr style="background: #f5f5f5;">
            <th style="padding: 8px; border: 1px solid #e8e8e8;">人员</th>
            <th style="padding: 8px; border: 1px solid #e8e8e8; width: 150px;">角色</th>
            <th style="padding: 8px; border: 1px solid #e8e8e8; width: 180px;">分摊比例</th>
            <th style="padding: 8px; border: 1px solid #e8e8e8; width: 150px;">金额(预估)</th>
            <th style="padding: 8px; border: 1px solid #e8e8e8; width: 60px;">操作</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let ctrl of details.controls; let i = index">
            <td style="padding: 8px; border: 1px solid #e8e8e8;">
              <nz-select style="width: 100%;" [formControl]="ctrl.get('userId')">
                <nz-option *ngFor="let m of teamMembers"
                  [nzValue]="m.userId"
                  [nzLabel]="m.userName + ' (' + m.userCode + ')'">
                </nz-option>
              </nz-select>
            </td>
            <td style="padding: 8px; border: 1px solid #e8e8e8;">
              <nz-select style="width: 100%;" [formControl]="ctrl.get('roleType')">
                <nz-option nzValue="DirectAgent" nzLabel="直接业务员"></nz-option>
                <nz-option nzValue="TeamLeader" nzLabel="团队主管"></nz-option>
                <nz-option nzValue="DepartmentHead" nzLabel="部门负责人"></nz-option>
                <nz-option nzValue="Other" nzLabel="其他"></nz-option>
              </nz-select>
            </td>
            <td style="padding: 8px; border: 1px solid #e8e8e8;">
              <nz-input-number style="width: 100%;" [formControl]="ctrl.get('allocationRatio')"
                [nzMin]="0" [nzMax]="1" [nzStep]="0.01"
                [nzFormatter]="pct" [nzParser]="pct">
              </nz-input-number>
            </td>
            <td style="padding: 8px; border: 1px solid #e8e8e8;">
              ¥{{ this.calcAmount(ctrl.get('allocationRatio')?.value).toFixed(2) }}
            </td>
            <td style="padding: 8px; border: 1px solid #e8e8e8; text-align: center;">
              <button nz-button nzType="text" nzDanger
                [disabled]="details.length <= 1"
                (click)="removeDetailRow(i)">删除</button>
            </td>
          </tr>
        </tbody>
      </table>
      <button nz-button nzType="dashed" style="width: 100%; margin-top: 8px;" (click)="addDetailRow()">
        <span nz-icon nzType="plus"></span> 添加分摊人员
      </button>
      ${this.currentRule ? `
      <div style="margin-top: 16px; padding: 12px; background: #fafafa; border-radius: 4px;">
        <label style="font-weight: 600;">当前生效规则：</label>
        <div style="font-size: 12px; color: #595959; margin-top: 4px;">
          生效日期: ${this.currentRule.effectiveStartDate.split('T')[0]}<br>
          ${this.currentRule.details.map(d =>
            d.userName + ' - ' + d.roleType + ' - ' + (d.allocationRatio * 100).toFixed(2) + '%'
          ).join(' / ')}
        </div>
      </div>` : ''}
    `;
  }

  pct = (value: number) => (value * 100).toFixed(2) + '%';

  loadCurrentRule(rule: AllocationRuleDto): void {
    this.details.clear();
    rule.details.forEach(d => {
      this.details.push(this.fb.group({
        userId: [d.userId, Validators.required],
        allocationRatio: [d.allocationRatio, [Validators.required, Validators.min(0), Validators.max(1)]],
        roleType: [d.roleType, Validators.required]
      }));
    });
  }

  addDetailRow(): void {
    this.details.push(this.fb.group({
      userId: [null, Validators.required],
      allocationRatio: [0, [Validators.required, Validators.min(0), Validators.max(1)]],
      roleType: ['DirectAgent', Validators.required]
    }));
  }

  removeDetailRow(index: number): void {
    this.details.removeAt(index);
  }

  getTotalRatio(): number {
    if (!this.details?.controls) return 0;
    return this.details.controls.reduce((sum, ctrl) => {
      const v = Number(ctrl.get('allocationRatio')?.value) || 0;
      return sum + v;
    }, 0);
  }

  calcAmount(ratio: number): number {
    return (this.policy?.commissionAmount || 0) * (Number(ratio) || 0);
  }

  submitAdjustment(): void {
    const dto: AdjustAllocationDto = {
      policyId: this.policy.policyId,
      adjustmentReason: this.form.get('adjustmentReason')?.value,
      effectiveFromDate: this.form.get('effectiveFromDate')?.value,
      newDetails: this.details.controls.map(ctrl => ({
        userId: ctrl.get('userId')?.value,
        allocationRatio: Number(ctrl.get('allocationRatio')?.value) || 0,
        roleType: ctrl.get('roleType')?.value
      }) as CreateAllocationRuleDetailDto[])
    };

    this.allocationService.adjust(dto).subscribe(res => {
      if (res.success) {
        this.msg.success('分摊比例调整成功，历史结算不会被修改');
      } else {
        this.msg.error(res.message);
      }
    });
  }
}
