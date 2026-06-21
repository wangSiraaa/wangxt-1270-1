using CommissionSettlement.Enums;
using CommissionSettlement.Models;
using Microsoft.EntityFrameworkCore;

namespace CommissionSettlement.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<AllocationRule> AllocationRules => Set<AllocationRule>();
    public DbSet<AllocationRuleDetail> AllocationRuleDetails => Set<AllocationRuleDetail>();
    public DbSet<AllocationAdjustment> AllocationAdjustments => Set<AllocationAdjustment>();
    public DbSet<MonthlySettlement> MonthlySettlements => Set<MonthlySettlement>();
    public DbSet<SettlementSnapshot> SettlementSnapshots => Set<SettlementSnapshot>();
    public DbSet<ClawbackRecord> ClawbackRecords => Set<ClawbackRecord>();
    public DbSet<PreTaxDeduction> PreTaxDeductions => Set<PreTaxDeduction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users", "cs");
            b.HasKey(e => e.UserId);
            b.HasIndex(e => e.UserCode).IsUnique();
            b.Property(e => e.Role).HasConversion<string>();
            b.HasMany(e => e.Subordinates).WithOne(e => e.ParentUser).HasForeignKey(e => e.ParentUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Policy>(b =>
        {
            b.ToTable("Policies", "cs");
            b.HasKey(e => e.PolicyId);
            b.HasIndex(e => e.PolicyNo).IsUnique();
            b.Property(e => e.Status).HasConversion<string>();
            b.Property(e => e.CancellationType).HasConversion<string>();
            b.Property(e => e.Premium).HasColumnType("DECIMAL(18,2)");
            b.Property(e => e.CommissionRate).HasColumnType("DECIMAL(9,4)");
            b.Property(e => e.CommissionAmount).HasColumnType("DECIMAL(18,2)");
            b.Property(e => e.RowVersion).IsRowVersion();
            b.HasOne(e => e.AgentUser).WithMany(e => e.Policies).HasForeignKey(e => e.AgentUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AllocationRule>(b =>
        {
            b.ToTable("AllocationRules", "cs");
            b.HasKey(e => e.RuleId);
            b.HasOne(e => e.Policy).WithMany(e => e.AllocationRules).HasForeignKey(e => e.PolicyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AllocationRuleDetail>(b =>
        {
            b.ToTable("AllocationRuleDetails", "cs");
            b.HasKey(e => e.DetailId);
            b.Property(e => e.AllocationRatio).HasColumnType("DECIMAL(9,4)");
            b.Property(e => e.AllocatedAmount).HasColumnType("DECIMAL(18,2)");
            b.HasOne(e => e.Rule).WithMany(e => e.Details).HasForeignKey(e => e.RuleId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(e => e.User).WithMany(e => e.AllocationDetails).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AllocationAdjustment>(b =>
        {
            b.ToTable("AllocationAdjustments", "cs");
            b.HasKey(e => e.AdjustmentId);
            b.HasOne(e => e.Policy).WithMany(e => e.AllocationAdjustments).HasForeignKey(e => e.PolicyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.OldRule).WithMany(e => e.OldAdjustments).HasForeignKey(e => e.OldRuleId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.NewRule).WithMany(e => e.NewAdjustments).HasForeignKey(e => e.NewRuleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MonthlySettlement>(b =>
        {
            b.ToTable("MonthlySettlements", "cs");
            b.HasKey(e => e.SettlementId);
            b.HasIndex(e => new { e.SettlementMonth, e.UserId }).IsUnique();
            b.Property(e => e.Status).HasConversion<string>();
            b.Property(e => e.TotalCommission).HasColumnType("DECIMAL(18,2)");
            b.Property(e => e.TotalClawback).HasColumnType("DECIMAL(18,2)");
            b.Property(e => e.TotalPreTaxDeduction).HasColumnType("DECIMAL(18,2)");
            b.Property(e => e.TaxableIncome).HasColumnType("DECIMAL(18,2)");
            b.Property(e => e.IncomeTax).HasColumnType("DECIMAL(18,2)");
            b.Property(e => e.NetPayable).HasColumnType("DECIMAL(18,2)");
            b.HasOne(e => e.User).WithMany(e => e.Settlements).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SettlementSnapshot>(b =>
        {
            b.ToTable("SettlementSnapshots", "cs");
            b.HasKey(e => e.SnapshotId);
            b.Property(e => e.Premium).HasColumnType("DECIMAL(18,2)");
            b.Property(e => e.CommissionRate).HasColumnType("DECIMAL(9,4)");
            b.Property(e => e.OriginalCommission).HasColumnType("DECIMAL(18,2)");
            b.Property(e => e.AllocationRatio).HasColumnType("DECIMAL(9,4)");
            b.Property(e => e.AllocatedCommission).HasColumnType("DECIMAL(18,2)");
            b.Property(e => e.ClawbackAmount).HasColumnType("DECIMAL(18,2)");
            b.Property(e => e.PreTaxDeduction).HasColumnType("DECIMAL(18,2)");
            b.HasOne(e => e.Settlement).WithMany(e => e.Snapshots).HasForeignKey(e => e.SettlementId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(e => e.Policy).WithMany(e => e.SettlementSnapshots).HasForeignKey(e => e.PolicyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClawbackRecord>(b =>
        {
            b.ToTable("ClawbackRecords", "cs");
            b.HasKey(e => e.ClawbackId);
            b.Property(e => e.ClawbackType).HasConversion<string>();
            b.Property(e => e.ClawbackAmount).HasColumnType("DECIMAL(18,2)");
            b.HasOne(e => e.Policy).WithMany(e => e.ClawbackRecords).HasForeignKey(e => e.PolicyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PreTaxDeduction>(b =>
        {
            b.ToTable("PreTaxDeductions", "cs");
            b.HasKey(e => e.DeductionId);
            b.Property(e => e.DeductionAmount).HasColumnType("DECIMAL(18,2)");
            b.HasOne(e => e.User).WithMany(e => e.PreTaxDeductions).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
