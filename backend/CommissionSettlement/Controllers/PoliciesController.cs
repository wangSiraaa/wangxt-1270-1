using CommissionSettlement.Common;
using CommissionSettlement.Dtos;
using CommissionSettlement.Enums;
using CommissionSettlement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CommissionSettlement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PoliciesController : ControllerBase
{
    private readonly IPolicyService _policyService;

    public PoliciesController(IPolicyService policyService)
    {
        _policyService = policyService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Agent,Supervisor,Finance")]
    public async Task<ApiResponse<PolicyDto>> Create([FromBody] CreatePolicyDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var policy = await _policyService.CreatePolicyAsync(dto, cancellationToken);
            return ApiResponse<PolicyDto>.Ok(policy);
        }
        catch (Exception ex)
        {
            return ApiResponse<PolicyDto>.Fail(ex.Message);
        }
    }

    [HttpPut("{policyId}")]
    [Authorize(Roles = "Admin,Finance,Supervisor")]
    public async Task<ApiResponse<PolicyDto>> Update(Guid policyId, [FromBody] UpdatePolicyDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var policy = await _policyService.UpdatePolicyAsync(policyId, dto, cancellationToken);
            return ApiResponse<PolicyDto>.Ok(policy);
        }
        catch (Exception ex)
        {
            return ApiResponse<PolicyDto>.Fail(ex.Message);
        }
    }

    [HttpPost("{policyId}/sign")]
    [Authorize(Roles = "Admin,Finance,Supervisor")]
    public async Task<ApiResponse<PolicyDto>> Sign(Guid policyId, [FromQuery] DateTime signedAt, CancellationToken cancellationToken)
    {
        try
        {
            var policy = await _policyService.SignPolicyAsync(policyId, signedAt, cancellationToken);
            return ApiResponse<PolicyDto>.Ok(policy);
        }
        catch (Exception ex)
        {
            return ApiResponse<PolicyDto>.Fail(ex.Message);
        }
    }

    [HttpPost("{policyId}/effective")]
    [Authorize(Roles = "Admin,Finance,Supervisor")]
    public async Task<ApiResponse<PolicyDto>> MakeEffective(Guid policyId, [FromQuery] DateTime effectiveAt, CancellationToken cancellationToken)
    {
        try
        {
            var policy = await _policyService.MakeEffectiveAsync(policyId, effectiveAt, cancellationToken);
            return ApiResponse<PolicyDto>.Ok(policy);
        }
        catch (Exception ex)
        {
            return ApiResponse<PolicyDto>.Fail(ex.Message);
        }
    }

    [HttpPost("{policyId}/cancel")]
    [Authorize(Roles = "Admin,Finance,Supervisor")]
    public async Task<ApiResponse<PolicyDto>> Cancel(
        Guid policyId,
        [FromQuery] CancellationType cancellationType,
        [FromQuery] DateTime cancelledAt,
        [FromBody] string? reason,
        CancellationToken cancellationToken)
    {
        try
        {
            var policy = await _policyService.CancelPolicyAsync(policyId, cancellationType, reason, cancelledAt, cancellationToken);
            return ApiResponse<PolicyDto>.Ok(policy);
        }
        catch (Exception ex)
        {
            return ApiResponse<PolicyDto>.Fail(ex.Message);
        }
    }

    [HttpGet("{policyId}")]
    public async Task<ApiResponse<PolicyDto>> GetById(Guid policyId, CancellationToken cancellationToken)
    {
        var policy = await _policyService.GetPolicyByIdAsync(policyId, cancellationToken);
        return policy == null
            ? ApiResponse<PolicyDto>.Fail("保单不存在")
            : ApiResponse<PolicyDto>.Ok(policy);
    }

    [HttpGet("query")]
    public async Task<ApiResponse<PagedResult<PolicyDto>>> Query([FromQuery] PolicyQueryDto query, CancellationToken cancellationToken)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);

        if (role == UserRole.Agent.ToString() && !query.AgentUserId.HasValue)
        {
            query.AgentUserId = userId;
        }

        var result = await _policyService.QueryPoliciesAsync(query, cancellationToken);
        return ApiResponse<PagedResult<PolicyDto>>.Ok(result);
    }

    [HttpGet("{policyId}/can-commission")]
    public async Task<ApiResponse<bool>> CanCommission(Guid policyId, CancellationToken cancellationToken)
    {
        var can = await _policyService.CanCommissionAsync(policyId, cancellationToken);
        return ApiResponse<bool>.Ok(can);
    }
}
