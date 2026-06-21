# 保险经纪佣金结算系统

基于 **ASP.NET Core 8 + SQL Server** 后端和 **Angular 17 + NG-ZORRO** 前端的全栈佣金结算管理系统。

## 核心业务规则

| 规则 | 说明 |
|------|------|
| 未生效保单不计佣 | 只有 `Effective` 状态且生效日已过的保单才计入佣金 |
| 犹豫期退保自动冲回 | 15天犹豫期内退保，佣金100%冲减；犹豫期后退保，冲减50% |
| 分摊调整不追溯历史 | 主管调整分摊比例只影响未来结算，已生成的历史结算单保持不变 |
| 调整原因必留痕 | 每次分摊比例调整必须填写原因，完整记录审计历史 |
| 可追溯结算快照 | 每条结算单保存当时的保单、分摊、冲减快照，支持永久追溯 |

## 角色与权限

| 角色 | 账号 | 权限 |
|------|------|------|
| 业务员 | agent01 / agent02 | 查看自己的保单明细和结算单 |
| 团队主管 | sup01 | 调整团队保单的分摊比例（需填写原因），查看团队结算 |
| 财务 | finance01 | 录入保单/扣款、生成月度结算、审批、标记已支付 |
| 管理员 | admin | 全部权限 |

## 项目结构

```
1270/
├── database/init.sql              # SQL Server 建库建表脚本
├── backend/CommissionSettlement/  # ASP.NET Core 8 后端
│   ├── Program.cs                 # 启动入口（端口 5000）
│   ├── Models/                    # EF Core 实体
│   ├── Data/AppDbContext.cs       # 数据库上下文
│   ├── Services/                  # 核心业务服务
│   │   ├── PolicyService.cs       # 保单生命周期管理
│   │   ├── AllocationService.cs   # 分摊规则与调整审计
│   │   ├── SettlementService.cs   # 月度结算引擎
│   │   ├── DeductionService.cs    # 税前扣款
│   │   └── AuthService.cs         # 认证与用户
│   ├── Controllers/               # REST API
│   ├── Dtos/                      # 数据传输对象
│   └── Enums/                     # 枚举定义
└── frontend/                      # Angular 17 + NG-ZORRO 前端
    ├── src/app/
    │   ├── services/              # API 服务层
    │   ├── pages/                 # 业务员/主管/财务视图
    │   ├── components/            # 可追溯结算表等可复用组件
    │   ├── interceptors/          # JWT 拦截器
    │   ├── guards/                # 角色路由守卫
    │   └── models/                # TypeScript 类型
    └── proxy.conf.json            # 开发代理（转发 /api 到 5000）
```

## 快速启动

### 1. 初始化数据库

使用 SQL Server Management Studio 或 `sqlcmd` 执行：
```bash
sqlcmd -S localhost -i database/init.sql
```

根据实际环境修改 `backend/CommissionSettlement/appsettings.json` 中的连接字符串。

### 2. 启动后端 (http://localhost:5000)

```bash
cd backend/CommissionSettlement
dotnet restore
dotnet run
```

启动后自动执行 `EnsureCreated` 建表并注入 5 个演示用户。
Swagger 文档：http://localhost:5000/swagger

### 3. 启动前端 (http://localhost:4200)

```bash
cd frontend
npm install
npm start
```

访问 http://localhost:4200 ，使用演示账号登录即可。

## 核心 API

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | /api/auth/login | 登录获取 JWT |
| GET  | /api/policies/query | 分页查询保单（业务员仅能看到自己的） |
| POST | /api/policies/{id}/effective | 标记保单生效 |
| POST | /api/policies/{id}/cancel | 退保/取消（自动生成冲减流水） |
| GET  | /api/allocations/policy/{id}/history | 查询保单分摊调整历史 |
| POST | /api/allocations/adjust | 调整分摊比例（原因必填，不追溯历史） |
| POST | /api/settlements/generate | 财务批量生成月度结算单 |
| GET  | /api/settlements/{id}/snapshots | 获取结算单的可追溯快照明细 |
| POST | /api/deductions | 录入税前扣款 |

## 结算计算流程

```
月度生效保单 × 佣金率 = 原始佣金
    ↓
按分摊比例分配到各人员
    ↓
- 本月退保冲减（犹豫期100%，其他50%）
- 本月税前扣款（社保、公积金等）
    ↓
应纳税所得额 × 累进税率 = 个税
    ↓
实发金额
```

每张结算单的每一条保单明细都保存快照（包含当时的分摊规则JSON），保证即使后续保单、分摊发生变化，历史结算数据也能完整追溯。
