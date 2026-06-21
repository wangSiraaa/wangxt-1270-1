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
public class SettlementsController : ControllerBase
{
    private readonly ISettlementService _settlementService;

    public SettlementsController(ISettlementService settlementService)
    {
        _settlementService = settlementService;
    }

    [HttpPost("generate")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<ApiResponse<List<MonthlySettlementDto>>> Generate(
        [FromBody] GenerateSettlementDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var generatedBy = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
            var settlements = await _settlementService.GenerateMonthlySettlementsAsync(dto.SettlementMonth, generatedBy, cancellationToken);
            return ApiResponse<List<MonthlySettlementDto>>.Ok(settlements);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<MonthlySettlementDto>>.Fail(ex.Message);
        }
    }

    [HttpGet("{settlementId}")]
    public async Task<ApiResponse<MonthlySettlementDto>> GetById(
        Guid settlementId,
        [FromQuery] bool includeSnapshots = true,
        CancellationToken cancellationToken = default)
    {
        var settlement = await _settlementService.GetSettlementByIdAsync(settlementId, includeSnapshots, cancellationToken);
        if (settlement == null)
            return ApiResponse<MonthlySettlementDto>.Fail("结算单不存在");

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role != "Admin" && role != "Finance" && role != "Supervisor" && settlement.UserId != userId)
            return ApiResponse<MonthlySettlementDto>.Fail("无权查看此结算单");

        return ApiResponse<MonthlySettlementDto>.Ok(settlement);
    }

    [HttpGet("query")]
    public async Task<ApiResponse<PagedResult<MonthlySettlementDto>>> Query(
        [FromQuery] SettlementQueryDto query,
        CancellationToken cancellationToken)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);

        if (role == "Agent" && !query.UserId.HasValue)
            query.UserId = userId;

        var result = await _settlementService.QuerySettlementsAsync(query, cancellationToken);
        return ApiResponse<PagedResult<MonthlySettlementDto>>.Ok(result);
    }

    [HttpGet("mine")]
    public async Task<ApiResponse<List<MonthlySettlementDto>>> GetMySettlements(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
        var settlements = await _settlementService.GetUserSettlementsAsync(userId, cancellationToken);
        return ApiResponse<List<MonthlySettlementDto>>.Ok(settlements);
    }

    [HttpGet("{settlementId}/snapshots")]
    public async Task<ApiResponse<List<SettlementSnapshotDto>>> GetSnapshots(Guid settlementId, CancellationToken cancellationToken)
    {
        var snapshots = await _settlementService.GetSettlementSnapshotsAsync(settlementId, cancellationToken);
        return ApiResponse<List<SettlementSnapshotDto>>.Ok(snapshots);
    }

    [HttpPost("{settlementId}/approve")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<ApiResponse<MonthlySettlementDto>> Approve(Guid settlementId, CancellationToken cancellationToken)
    {
        try
        {
            var approvedBy = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
            var result = await _settlementService.ApproveSettlementAsync(settlementId, approvedBy, cancellationToken);
            return ApiResponse<MonthlySettlementDto>.Ok(result);
        }
        catch (Exception ex)
        {
            return ApiResponse<MonthlySettlementDto>.Fail(ex.Message);
        }
    }

    [HttpPost("{settlementId}/reject")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<ApiResponse<MonthlySettlementDto>> Reject(
        Guid settlementId,
        [FromBody] string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            var rejectedBy = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
            var result = await _settlementService.RejectSettlementAsync(settlementId, rejectedBy, reason, cancellationToken);
            return ApiResponse<MonthlySettlementDto>.Ok(result);
        }
        catch (Exception ex)
        {
            return ApiResponse<MonthlySettlementDto>.Fail(ex.Message);
        }
    }

    [HttpPost("{settlementId}/mark-paid")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<ApiResponse<MonthlySettlementDto>> MarkPaid(Guid settlementId, CancellationToken cancellationToken)
    {
        try
        {
            var paidBy = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
            var result = await _settlementService.MarkAsPaidAsync(settlementId, paidBy, cancellationToken);
            return ApiResponse<MonthlySettlementDto>.Ok(result);
        }
        catch (Exception ex)
        {
            return ApiResponse<MonthlySettlementDto>.Fail(ex.Message);
        }
    }

    [HttpGet("clawbacks")]
    [Authorize(Roles = "Admin,Finance,Supervisor")]
    public async Task<ApiResponse<List<ClawbackRecordDto>>> GetClawbacks(
        [FromQuery] Guid? policyId,
        CancellationToken cancellationToken)
    {
        var records = await _settlementService.GetClawbackRecordsAsync(policyId, cancellationToken);
        return ApiResponse<List<ClawbackRecordDto>>.Ok(records);
    }
}
