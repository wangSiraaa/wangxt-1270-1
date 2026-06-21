using CommissionSettlement.Data;
using CommissionSettlement.Dtos;
using CommissionSettlement.Models;
using Microsoft.EntityFrameworkCore;

namespace CommissionSettlement.Services;

public interface IDeductionService
{
    Task<PreTaxDeductionDto> CreateDeductionAsync(CreatePreTaxDeductionDto dto, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<List<PreTaxDeductionDto>> GetDeductionsByMonthAsync(string deductionMonth, CancellationToken cancellationToken = default);
    Task<List<PreTaxDeductionDto>> GetDeductionsByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteDeductionAsync(Guid deductionId, CancellationToken cancellationToken = default);
}

public class DeductionService : IDeductionService
{
    private readonly AppDbContext _context;

    public DeductionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PreTaxDeductionDto> CreateDeductionAsync(CreatePreTaxDeductionDto dto, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object?[] { dto.UserId }, cancellationToken);
        if (user == null)
            throw new InvalidOperationException("用户不存在");

        if (dto.DeductionAmount <= 0)
            throw new InvalidOperationException("扣款金额必须大于0");

        var deduction = new PreTaxDeduction
        {
            DeductionId = Guid.NewGuid(),
            UserId = dto.UserId,
            DeductionType = dto.DeductionType,
            DeductionAmount = dto.DeductionAmount,
            DeductionMonth = dto.DeductionMonth,
            Description = dto.Description,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.PreTaxDeductions.Add(deduction);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(deduction, user);
    }

    public async Task<List<PreTaxDeductionDto>> GetDeductionsByMonthAsync(string deductionMonth, CancellationToken cancellationToken = default)
    {
        var deductions = await _context.PreTaxDeductions
            .Include(d => d.User)
            .Where(d => d.DeductionMonth == deductionMonth)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        return deductions.Select(d => MapToDto(d, d.User)).ToList();
    }

    public async Task<List<PreTaxDeductionDto>> GetDeductionsByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var deductions = await _context.PreTaxDeductions
            .Include(d => d.User)
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.DeductionMonth)
            .ThenByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        return deductions.Select(d => MapToDto(d, d.User)).ToList();
    }

    public async Task<bool> DeleteDeductionAsync(Guid deductionId, CancellationToken cancellationToken = default)
    {
        var deduction = await _context.PreTaxDeductions.FindAsync(new object?[] { deductionId }, cancellationToken);
        if (deduction == null) return false;

        _context.PreTaxDeductions.Remove(deduction);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static PreTaxDeductionDto MapToDto(PreTaxDeduction d, User? u) => new(
        d.DeductionId,
        d.UserId,
        u?.UserName ?? string.Empty,
        d.DeductionType,
        d.DeductionAmount,
        d.DeductionMonth,
        d.Description,
        d.CreatedAt
    );
}
