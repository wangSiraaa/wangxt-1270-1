using CommissionSettlement.Common;
using CommissionSettlement.Dtos;
using CommissionSettlement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CommissionSettlement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Finance")]
public class DeductionsController : ControllerBase
{
    private readonly IDeductionService _deductionService;

    public DeductionsController(IDeductionService deductionService)
    {
        _deductionService = deductionService;
    }

    [HttpPost]
    public async Task<ApiResponse<PreTaxDeductionDto>> Create([FromBody] CreatePreTaxDeductionDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var createdBy = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
            var deduction = await _deductionService.CreateDeductionAsync(dto, createdBy, cancellationToken);
            return ApiResponse<PreTaxDeductionDto>.Ok(deduction);
        }
        catch (Exception ex)
        {
            return ApiResponse<PreTaxDeductionDto>.Fail(ex.Message);
        }
    }

    [HttpGet("month/{deductionMonth}")]
    public async Task<ApiResponse<List<PreTaxDeductionDto>>> GetByMonth(string deductionMonth, CancellationToken cancellationToken)
    {
        var deductions = await _deductionService.GetDeductionsByMonthAsync(deductionMonth, cancellationToken);
        return ApiResponse<List<PreTaxDeductionDto>>.Ok(deductions);
    }

    [HttpGet("user/{userId}")]
    public async Task<ApiResponse<List<PreTaxDeductionDto>>> GetByUser(Guid userId, CancellationToken cancellationToken)
    {
        var deductions = await _deductionService.GetDeductionsByUserAsync(userId, cancellationToken);
        return ApiResponse<List<PreTaxDeductionDto>>.Ok(deductions);
    }

    [HttpDelete("{deductionId}")]
    public async Task<ApiResponse<bool>> Delete(Guid deductionId, CancellationToken cancellationToken)
    {
        var result = await _deductionService.DeleteDeductionAsync(deductionId, cancellationToken);
        return result
            ? ApiResponse<bool>.Ok(true)
            : ApiResponse<bool>.Fail("扣款记录不存在");
    }
}
