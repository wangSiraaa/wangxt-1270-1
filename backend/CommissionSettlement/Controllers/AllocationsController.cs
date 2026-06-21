using CommissionSettlement.Common;
using CommissionSettlement.Dtos;
using CommissionSettlement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CommissionSettlement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AllocationsController : ControllerBase
{
    private readonly IAllocationService _allocationService;

    public AllocationsController(IAllocationService allocationService)
    {
        _allocationService = allocationService;
    }

    [HttpGet("policy/{policyId}/active")]
    public async Task<ApiResponse<AllocationRuleDto>> GetActiveRule(Guid policyId, CancellationToken cancellationToken)
    {
        var rule = await _allocationService.GetActiveRuleByPolicyAsync(policyId, cancellationToken);
        return rule == null
            ? ApiResponse<AllocationRuleDto>.Fail("未找到有效的分摊规则")
            : ApiResponse<AllocationRuleDto>.Ok(rule);
    }

    [HttpGet("policy/{policyId}/rules")]
    public async Task<ApiResponse<List<AllocationRuleDto>>> GetPolicyRules(Guid policyId, CancellationToken cancellationToken)
    {
        var rules = await _allocationService.GetPolicyRulesAsync(policyId, cancellationToken);
        return ApiResponse<List<AllocationRuleDto>>.Ok(rules);
    }

    [HttpGet("policy/{policyId}/history")]
    [Authorize(Roles = "Admin,Supervisor,Finance")]
    public async Task<ApiResponse<List<AllocationAdjustmentDto>>> GetAdjustmentHistory(Guid policyId, CancellationToken cancellationToken)
    {
        var history = await _allocationService.GetPolicyAdjustmentHistoryAsync(policyId, cancellationToken);
        return ApiResponse<List<AllocationAdjustmentDto>>.Ok(history);
    }

    [HttpPost("adjust")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<ApiResponse<AllocationAdjustmentDto>> Adjust([FromBody] AdjustAllocationDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var adjustedBy = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
            var result = await _allocationService.AdjustAllocationAsync(dto, adjustedBy, cancellationToken);
            return ApiResponse<AllocationAdjustmentDto>.Ok(result);
        }
        catch (Exception ex)
        {
            return ApiResponse<AllocationAdjustmentDto>.Fail(ex.Message);
        }
    }

    [HttpGet("policy/{policyId}/rule-for-month")]
    [Authorize(Roles = "Admin,Finance,Supervisor")]
    public async Task<ApiResponse<AllocationRuleDto>> GetRuleForMonth(
        Guid policyId,
        [FromQuery] DateTime settlementMonth,
        CancellationToken cancellationToken)
    {
        var rule = await _allocationService.GetRuleForMonthAsync(policyId, settlementMonth, cancellationToken);
        return rule == null
            ? ApiResponse<AllocationRuleDto>.Fail("未找到该月份的分摊规则")
            : ApiResponse<AllocationRuleDto>.Ok(rule);
    }
}
