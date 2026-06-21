export type UserRole = 'Agent' | 'Supervisor' | 'Finance' | 'Admin';
export type PolicyStatus = 'Draft' | 'Pending' | 'Signed' | 'Effective' | 'CoolingPeriod' | 'Cancelled' | 'Surrendered';
export type CancellationType = 'CoolingPeriod' | 'NormalSurrender' | 'Other';
export type SettlementStatus = 'Draft' | 'Generated' | 'Approved' | 'Paid' | 'Rejected';
export type ClawbackType = 'CoolingPeriodCancel' | 'NormalSurrender' | 'Adjustment' | 'Other';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
  errors: string[] | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
}

export interface User {
  userId: string;
  userCode: string;
  userName: string;
  role: UserRole;
  parentUserId: string | null;
  parentUserName: string | null;
  isActive: boolean;
}

export interface UserLoginDto {
  userCode: string;
  password: string;
}

export interface UserLoginResponse {
  userId: string;
  userCode: string;
  userName: string;
  role: UserRole;
  token: string;
  expiresAt: string;
}

export interface Policy {
  policyId: string;
  policyNo: string;
  productName: string;
  policyHolder: string;
  insured: string;
  premium: number;
  commissionRate: number;
  commissionAmount: number;
  status: PolicyStatus;
  agentUserId: string;
  agentUserName: string | null;
  signedAt: string | null;
  effectiveAt: string | null;
  coolingPeriodEndAt: string | null;
  cancelledAt: string | null;
  cancellationType: CancellationType | null;
}

export interface CreatePolicyDto {
  policyNo: string;
  productName: string;
  policyHolder: string;
  insured: string;
  premium: number;
  commissionRate: number | null;
  agentUserId: string;
}

export interface PolicyQueryDto {
  pageIndex?: number;
  pageSize?: number;
  sortField?: string | null;
  sortOrder?: string | null;
  policyNo?: string | null;
  status?: PolicyStatus | null;
  agentUserId?: string | null;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
}

export interface AllocationRuleDetailDto {
  detailId: string;
  userId: string;
  userName: string;
  userCode: string;
  allocationRatio: number;
  roleType: string;
  allocatedAmount: number;
}

export interface AllocationRuleDto {
  ruleId: string;
  policyId: string;
  policyNo: string;
  effectiveStartDate: string;
  effectiveEndDate: string | null;
  isActive: boolean;
  details: AllocationRuleDetailDto[];
}

export interface AllocationAdjustmentDto {
  adjustmentId: string;
  policyId: string;
  policyNo: string;
  adjustmentReason: string;
  effectiveFromDate: string;
  createdAt: string;
  adjustedByUserId: string;
  adjustedByUserName: string;
  oldDetails: AllocationRuleDetailDto[] | null;
  newDetails: AllocationRuleDetailDto[];
}

export interface CreateAllocationRuleDetailDto {
  userId: string;
  allocationRatio: number;
  roleType: string;
}

export interface AdjustAllocationDto {
  policyId: string;
  adjustmentReason: string;
  effectiveFromDate: string;
  newDetails: CreateAllocationRuleDetailDto[];
}

export interface SettlementSnapshotDto {
  snapshotId: string;
  settlementId: string;
  policyId: string;
  policyNo: string;
  policyStatus: string;
  premium: number;
  commissionRate: number;
  originalCommission: number;
  allocationRatio: number;
  allocatedCommission: number;
  clawbackAmount: number;
  preTaxDeduction: number;
  ruleSnapshot: string | null;
  createdAt: string;
}

export interface MonthlySettlementDto {
  settlementId: string;
  settlementMonth: string;
  userId: string;
  userName: string;
  userCode: string;
  totalCommission: number;
  totalClawback: number;
  totalPreTaxDeduction: number;
  taxableIncome: number;
  incomeTax: number;
  netPayable: number;
  status: SettlementStatus;
  generatedAt: string;
  approvedAt: string | null;
  paidAt: string | null;
  snapshots: SettlementSnapshotDto[] | null;
}

export interface SettlementQueryDto {
  pageIndex?: number;
  pageSize?: number;
  sortField?: string | null;
  sortOrder?: string | null;
  settlementMonth?: string | null;
  userId?: string | null;
  status?: SettlementStatus | null;
}

export interface GenerateSettlementDto {
  settlementMonth: string;
}

export interface ClawbackRecordDto {
  clawbackId: string;
  policyId: string;
  policyNo: string;
  clawbackType: ClawbackType;
  clawbackAmount: number;
  cancelledAt: string;
  reason: string | null;
  isCoolingPeriod: boolean;
  createdAt: string;
}

export interface PreTaxDeductionDto {
  deductionId: string;
  userId: string;
  userName: string;
  deductionType: string;
  deductionAmount: number;
  deductionMonth: string;
  description: string | null;
  createdAt: string;
}

export interface CreatePreTaxDeductionDto {
  userId: string;
  deductionType: string;
  deductionAmount: number;
  deductionMonth: string;
  description: string | null;
}
