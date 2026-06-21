USE [master];
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'CommissionSettlementDB')
BEGIN
    CREATE DATABASE [CommissionSettlementDB];
END
GO

USE [CommissionSettlementDB];
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'cs')
BEGIN
    EXEC('CREATE SCHEMA [cs] AUTHORIZATION [dbo]');
END
GO

IF OBJECT_ID(N'[cs].[Users]', 'U') IS NULL
BEGIN
    CREATE TABLE [cs].[Users] (
        [UserId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [UserCode] NVARCHAR(50) NOT NULL,
        [UserName] NVARCHAR(100) NOT NULL,
        [Role] NVARCHAR(20) NOT NULL,
        [ParentUserId] UNIQUEIDENTIFIER NULL,
        [DepartmentId] UNIQUEIDENTIFIER NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE UNIQUE INDEX [IX_Users_UserCode] ON [cs].[Users]([UserCode]);
    CREATE INDEX [IX_Users_ParentUserId] ON [cs].[Users]([ParentUserId]);
END
GO

IF OBJECT_ID(N'[cs].[Policies]', 'U') IS NULL
BEGIN
    CREATE TABLE [cs].[Policies] (
        [PolicyId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [PolicyNo] NVARCHAR(50) NOT NULL,
        [ProductName] NVARCHAR(200) NOT NULL,
        [PolicyHolder] NVARCHAR(100) NOT NULL,
        [Insured] NVARCHAR(100) NOT NULL,
        [Premium] DECIMAL(18,2) NOT NULL,
        [CommissionRate] DECIMAL(9,4) NOT NULL DEFAULT 0,
        [CommissionAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Status] NVARCHAR(20) NOT NULL,
        [AgentUserId] UNIQUEIDENTIFIER NOT NULL,
        [SignedAt] DATETIME2 NULL,
        [EffectiveAt] DATETIME2 NULL,
        [CoolingPeriodEndAt] DATETIME2 NULL,
        [CancelledAt] DATETIME2 NULL,
        [CancellationType] NVARCHAR(20) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [RowVersion] ROWVERSION NOT NULL
    );
    CREATE UNIQUE INDEX [IX_Policies_PolicyNo] ON [cs].[Policies]([PolicyNo]);
    CREATE INDEX [IX_Policies_AgentUserId] ON [cs].[Policies]([AgentUserId]);
    CREATE INDEX [IX_Policies_Status] ON [cs].[Policies]([Status]);
    CREATE INDEX [IX_Policies_EffectiveAt] ON [cs].[Policies]([EffectiveAt]);
END
GO

IF OBJECT_ID(N'[cs].[AllocationRules]', 'U') IS NULL
BEGIN
    CREATE TABLE [cs].[AllocationRules] (
        [RuleId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [PolicyId] UNIQUEIDENTIFIER NOT NULL,
        [EffectiveStartDate] DATE NOT NULL,
        [EffectiveEndDate] DATE NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedByUserId] UNIQUEIDENTIFIER NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE INDEX [IX_AllocationRules_PolicyId] ON [cs].[AllocationRules]([PolicyId]);
    CREATE INDEX [IX_AllocationRules_Effective] ON [cs].[AllocationRules]([PolicyId], [EffectiveStartDate]);
END
GO

IF OBJECT_ID(N'[cs].[AllocationRuleDetails]', 'U') IS NULL
BEGIN
    CREATE TABLE [cs].[AllocationRuleDetails] (
        [DetailId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [RuleId] UNIQUEIDENTIFIER NOT NULL,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [AllocationRatio] DECIMAL(9,4) NOT NULL,
        [RoleType] NVARCHAR(50) NOT NULL,
        [AllocatedAmount] DECIMAL(18,2) NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_AllocationRuleDetails_RuleId] ON [cs].[AllocationRuleDetails]([RuleId]);
    CREATE INDEX [IX_AllocationRuleDetails_UserId] ON [cs].[AllocationRuleDetails]([UserId]);
END
GO

IF OBJECT_ID(N'[cs].[AllocationAdjustments]', 'U') IS NULL
BEGIN
    CREATE TABLE [cs].[AllocationAdjustments] (
        [AdjustmentId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [PolicyId] UNIQUEIDENTIFIER NOT NULL,
        [OldRuleId] UNIQUEIDENTIFIER NULL,
        [NewRuleId] UNIQUEIDENTIFIER NOT NULL,
        [AdjustedByUserId] UNIQUEIDENTIFIER NOT NULL,
        [AdjustmentReason] NVARCHAR(500) NOT NULL,
        [EffectiveFromDate] DATE NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE INDEX [IX_AllocationAdjustments_PolicyId] ON [cs].[AllocationAdjustments]([PolicyId]);
    CREATE INDEX [IX_AllocationAdjustments_EffectiveFromDate] ON [cs].[AllocationAdjustments]([EffectiveFromDate]);
END
GO

IF OBJECT_ID(N'[cs].[MonthlySettlements]', 'U') IS NULL
BEGIN
    CREATE TABLE [cs].[MonthlySettlements] (
        [SettlementId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [SettlementMonth] NVARCHAR(7) NOT NULL,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [TotalCommission] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalClawback] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalPreTaxDeduction] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TaxableIncome] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [IncomeTax] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [NetPayable] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Status] NVARCHAR(20) NOT NULL,
        [GeneratedByUserId] UNIQUEIDENTIFIER NOT NULL,
        [GeneratedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ApprovedAt] DATETIME2 NULL,
        [ApprovedByUserId] UNIQUEIDENTIFIER NULL,
        [PaidAt] DATETIME2 NULL
    );
    CREATE UNIQUE INDEX [IX_MonthlySettlements_UserMonth] ON [cs].[MonthlySettlements]([SettlementMonth], [UserId]);
    CREATE INDEX [IX_MonthlySettlements_UserId] ON [cs].[MonthlySettlements]([UserId]);
    CREATE INDEX [IX_MonthlySettlements_Status] ON [cs].[MonthlySettlements]([Status]);
END
GO

IF OBJECT_ID(N'[cs].[SettlementSnapshots]', 'U') IS NULL
BEGIN
    CREATE TABLE [cs].[SettlementSnapshots] (
        [SnapshotId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [SettlementId] UNIQUEIDENTIFIER NOT NULL,
        [PolicyId] UNIQUEIDENTIFIER NOT NULL,
        [PolicyNo] NVARCHAR(50) NOT NULL,
        [PolicyStatus] NVARCHAR(20) NOT NULL,
        [Premium] DECIMAL(18,2) NOT NULL,
        [CommissionRate] DECIMAL(9,4) NOT NULL,
        [OriginalCommission] DECIMAL(18,2) NOT NULL,
        [AllocationRatio] DECIMAL(9,4) NOT NULL,
        [AllocatedCommission] DECIMAL(18,2) NOT NULL,
        [ClawbackAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [PreTaxDeduction] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [RuleSnapshot] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE INDEX [IX_SettlementSnapshots_SettlementId] ON [cs].[SettlementSnapshots]([SettlementId]);
    CREATE INDEX [IX_SettlementSnapshots_PolicyId] ON [cs].[SettlementSnapshots]([PolicyId]);
END
GO

IF OBJECT_ID(N'[cs].[ClawbackRecords]', 'U') IS NULL
BEGIN
    CREATE TABLE [cs].[ClawbackRecords] (
        [ClawbackId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [PolicyId] UNIQUEIDENTIFIER NOT NULL,
        [OriginalSettlementId] UNIQUEIDENTIFIER NULL,
        [ClawbackType] NVARCHAR(20) NOT NULL,
        [ClawbackAmount] DECIMAL(18,2) NOT NULL,
        [CancelledAt] DATETIME2 NOT NULL,
        [Reason] NVARCHAR(500) NULL,
        [IsCoolingPeriod] BIT NOT NULL DEFAULT 0,
        [AffectedSettlementId] UNIQUEIDENTIFIER NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE INDEX [IX_ClawbackRecords_PolicyId] ON [cs].[ClawbackRecords]([PolicyId]);
    CREATE INDEX [IX_ClawbackRecords_CancelledAt] ON [cs].[ClawbackRecords]([CancelledAt]);
END
GO

IF OBJECT_ID(N'[cs].[PreTaxDeductions]', 'U') IS NULL
BEGIN
    CREATE TABLE [cs].[PreTaxDeductions] (
        [DeductionId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [DeductionType] NVARCHAR(50) NOT NULL,
        [DeductionAmount] DECIMAL(18,2) NOT NULL,
        [DeductionMonth] NVARCHAR(7) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [CreatedByUserId] UNIQUEIDENTIFIER NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE INDEX [IX_PreTaxDeductions_UserId] ON [cs].[PreTaxDeductions]([UserId]);
    CREATE INDEX [IX_PreTaxDeductions_Month] ON [cs].[PreTaxDeductions]([DeductionMonth]);
END
GO
