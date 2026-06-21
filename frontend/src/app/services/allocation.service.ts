import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  ApiResponse, AllocationRuleDto, AllocationAdjustmentDto, AdjustAllocationDto
} from '../models';

@Injectable({ providedIn: 'root' })
export class AllocationService {
  private readonly apiUrl = `${environment.apiBaseUrl}/allocations`;

  constructor(private http: HttpClient) {}

  getActiveRule(policyId: string): Observable<ApiResponse<AllocationRuleDto>> {
    return this.http.get<ApiResponse<AllocationRuleDto>>(`${this.apiUrl}/policy/${policyId}/active`);
  }

  getPolicyRules(policyId: string): Observable<ApiResponse<AllocationRuleDto[]>> {
    return this.http.get<ApiResponse<AllocationRuleDto[]>>(`${this.apiUrl}/policy/${policyId}/rules`);
  }

  getAdjustmentHistory(policyId: string): Observable<ApiResponse<AllocationAdjustmentDto[]>> {
    return this.http.get<ApiResponse<AllocationAdjustmentDto[]>>(`${this.apiUrl}/policy/${policyId}/history`);
  }

  adjust(dto: AdjustAllocationDto): Observable<ApiResponse<AllocationAdjustmentDto>> {
    return this.http.post<ApiResponse<AllocationAdjustmentDto>>(`${this.apiUrl}/adjust`, dto);
  }

  getRuleForMonth(policyId: string, settlementMonth: string): Observable<ApiResponse<AllocationRuleDto>> {
    return this.http.get<ApiResponse<AllocationRuleDto>>(`${this.apiUrl}/policy/${policyId}/rule-for-month`, {
      params: { settlementMonth }
    });
  }
}
