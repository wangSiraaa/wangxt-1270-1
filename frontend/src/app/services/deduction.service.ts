import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PreTaxDeductionDto, CreatePreTaxDeductionDto } from '../models';

@Injectable({ providedIn: 'root' })
export class DeductionService {
  private readonly apiUrl = `${environment.apiBaseUrl}/deductions`;

  constructor(private http: HttpClient) {}

  create(dto: CreatePreTaxDeductionDto): Observable<ApiResponse<PreTaxDeductionDto>> {
    return this.http.post<ApiResponse<PreTaxDeductionDto>>(this.apiUrl, dto);
  }

  getByMonth(deductionMonth: string): Observable<ApiResponse<PreTaxDeductionDto[]>> {
    return this.http.get<ApiResponse<PreTaxDeductionDto[]>>(`${this.apiUrl}/month/${deductionMonth}`);
  }

  getByUser(userId: string): Observable<ApiResponse<PreTaxDeductionDto[]>> {
    return this.http.get<ApiResponse<PreTaxDeductionDto[]>>(`${this.apiUrl}/user/${userId}`);
  }

  delete(deductionId: string): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.apiUrl}/${deductionId}`);
  }
}
