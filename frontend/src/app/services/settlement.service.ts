import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  ApiResponse, PagedResult, MonthlySettlementDto, SettlementSnapshotDto,
  SettlementQueryDto, GenerateSettlementDto, ClawbackRecordDto, SettlementStatus
} from '../models';

@Injectable({ providedIn: 'root' })
export class SettlementService {
  private readonly apiUrl = `${environment.apiBaseUrl}/settlements`;

  constructor(private http: HttpClient) {}

  generate(dto: GenerateSettlementDto): Observable<ApiResponse<MonthlySettlementDto[]>> {
    return this.http.post<ApiResponse<MonthlySettlementDto[]>>(`${this.apiUrl}/generate`, dto);
  }

  getById(settlementId: string, includeSnapshots = true): Observable<ApiResponse<MonthlySettlementDto>> {
    return this.http.get<ApiResponse<MonthlySettlementDto>>(`${this.apiUrl}/${settlementId}`, {
      params: { includeSnapshots }
    });
  }

  query(query: SettlementQueryDto): Observable<ApiResponse<PagedResult<MonthlySettlementDto>>> {
    let params = new HttpParams();
    Object.entries(query).forEach(([key, value]) => {
      if (value !== null && value !== undefined && value !== '') {
        params = params.set(key, value as string);
      }
    });
    return this.http.get<ApiResponse<PagedResult<MonthlySettlementDto>>>(`${this.apiUrl}/query`, { params });
  }

  getMySettlements(): Observable<ApiResponse<MonthlySettlementDto[]>> {
    return this.http.get<ApiResponse<MonthlySettlementDto[]>>(`${this.apiUrl}/mine`);
  }

  getSnapshots(settlementId: string): Observable<ApiResponse<SettlementSnapshotDto[]>> {
    return this.http.get<ApiResponse<SettlementSnapshotDto[]>>(`${this.apiUrl}/${settlementId}/snapshots`);
  }

  approve(settlementId: string): Observable<ApiResponse<MonthlySettlementDto>> {
    return this.http.post<ApiResponse<MonthlySettlementDto>>(`${this.apiUrl}/${settlementId}/approve`, null);
  }

  reject(settlementId: string, reason: string): Observable<ApiResponse<MonthlySettlementDto>> {
    return this.http.post<ApiResponse<MonthlySettlementDto>>(`${this.apiUrl}/${settlementId}/reject`, reason);
  }

  markPaid(settlementId: string): Observable<ApiResponse<MonthlySettlementDto>> {
    return this.http.post<ApiResponse<MonthlySettlementDto>>(`${this.apiUrl}/${settlementId}/mark-paid`, null);
  }

  getClawbacks(policyId?: string): Observable<ApiResponse<ClawbackRecordDto[]>> {
    const params: any = {};
    if (policyId) params.policyId = policyId;
    return this.http.get<ApiResponse<ClawbackRecordDto[]>>(`${this.apiUrl}/clawbacks`, { params });
  }

  getStatusLabel(status: SettlementStatus): string {
    const map: Record<SettlementStatus, string> = {
      Draft: '草稿',
      Generated: '已生成',
      Approved: '已审批',
      Paid: '已支付',
      Rejected: '已驳回'
    };
    return map[status] || status;
  }

  getStatusColor(status: SettlementStatus): string {
    const map: Record<SettlementStatus, string> = {
      Draft: 'default',
      Generated: 'processing',
      Approved: 'success',
      Paid: 'success',
      Rejected: 'error'
    };
    return map[status] || 'default';
  }
}
