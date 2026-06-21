import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  ApiResponse, PagedResult, Policy, CreatePolicyDto, PolicyQueryDto,
  PolicyStatus, CancellationType
} from '../models';

@Injectable({ providedIn: 'root' })
export class PolicyService {
  private readonly apiUrl = `${environment.apiBaseUrl}/policies`;

  constructor(private http: HttpClient) {}

  create(dto: CreatePolicyDto): Observable<ApiResponse<Policy>> {
    return this.http.post<ApiResponse<Policy>>(this.apiUrl, dto);
  }

  update(policyId: string, dto: any): Observable<ApiResponse<Policy>> {
    return this.http.put<ApiResponse<Policy>>(`${this.apiUrl}/${policyId}`, dto);
  }

  sign(policyId: string, signedAt: string): Observable<ApiResponse<Policy>> {
    return this.http.post<ApiResponse<Policy>>(`${this.apiUrl}/${policyId}/sign`, null, {
      params: { signedAt }
    });
  }

  makeEffective(policyId: string, effectiveAt: string): Observable<ApiResponse<Policy>> {
    return this.http.post<ApiResponse<Policy>>(`${this.apiUrl}/${policyId}/effective`, null, {
      params: { effectiveAt }
    });
  }

  cancel(policyId: string, cancellationType: CancellationType, cancelledAt: string, reason?: string): Observable<ApiResponse<Policy>> {
    return this.http.post<ApiResponse<Policy>>(`${this.apiUrl}/${policyId}/cancel`, reason, {
      params: { cancellationType, cancelledAt }
    });
  }

  getById(policyId: string): Observable<ApiResponse<Policy>> {
    return this.http.get<ApiResponse<Policy>>(`${this.apiUrl}/${policyId}`);
  }

  query(query: PolicyQueryDto): Observable<ApiResponse<PagedResult<Policy>>> {
    let params = new HttpParams();
    Object.entries(query).forEach(([key, value]) => {
      if (value !== null && value !== undefined && value !== '') {
        params = params.set(key, value as string);
      }
    });
    return this.http.get<ApiResponse<PagedResult<Policy>>>(`${this.apiUrl}/query`, { params });
  }

  canCommission(policyId: string): Observable<ApiResponse<boolean>> {
    return this.http.get<ApiResponse<boolean>>(`${this.apiUrl}/${policyId}/can-commission`);
  }

  getPolicyStatusLabel(status: PolicyStatus): string {
    const map: Record<PolicyStatus, string> = {
      Draft: '草稿',
      Pending: '待签署',
      Signed: '已签署',
      Effective: '已生效',
      CoolingPeriod: '犹豫期',
      Cancelled: '已取消',
      Surrendered: '已退保'
    };
    return map[status] || status;
  }

  getPolicyStatusColor(status: PolicyStatus): string {
    const map: Record<PolicyStatus, string> = {
      Draft: 'default',
      Pending: 'processing',
      Signed: 'processing',
      Effective: 'success',
      CoolingPeriod: 'warning',
      Cancelled: 'error',
      Surrendered: 'error'
    };
    return map[status] || 'default';
  }
}
