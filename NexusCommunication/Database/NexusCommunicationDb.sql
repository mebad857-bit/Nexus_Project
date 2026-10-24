/* ===========================================================================
   Nexus Communication System — SQL Server schema
   NOTE: The application creates this schema automatically on first run
   (Database:AutoCreate = true in appsettings.json). Use this script only if
   you prefer to create the tables manually (set AutoCreate to false first).
   Demo data is inserted by the app itself (passwords are PBKDF2-hashed).
   =========================================================================== */
IF DB_ID(N'NexusCommunicationDb') IS NULL CREATE DATABASE [NexusCommunicationDb];
GO
USE [NexusCommunicationDb];
GO

CREATE TABLE dbo.Users (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Username nvarchar(50) NOT NULL,
    PasswordHash nvarchar(300) NOT NULL,
    Name nvarchar(200) NOT NULL,
    Role nvarchar(20) NOT NULL,
    CreatedAt datetime2 NOT NULL
);
CREATE UNIQUE INDEX IX_Users_Username ON dbo.Users(Username);

CREATE TABLE dbo.RetailShops (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Code nvarchar(30) NOT NULL,
    Name nvarchar(200) NOT NULL,
    City nvarchar(100) NOT NULL,
    Address nvarchar(400) NOT NULL,
    Phone nvarchar(50) NOT NULL,
    Manager nvarchar(200) NULL,
    ImageUrl nvarchar(300) NULL
);
CREATE UNIQUE INDEX IX_RetailShops_Code ON dbo.RetailShops(Code);

CREATE TABLE dbo.Employees (
    Id int IDENTITY(1,1) PRIMARY KEY,
    EmpCode nvarchar(30) NOT NULL,
    UserId int NULL REFERENCES dbo.Users(Id),
    Name nvarchar(200) NOT NULL,
    Email nvarchar(200) NOT NULL,
    Phone nvarchar(50) NOT NULL,
    Department nvarchar(30) NOT NULL,
    Designation nvarchar(100) NOT NULL,
    ShopId int NULL REFERENCES dbo.RetailShops(Id),
    Status nvarchar(20) NOT NULL,
    JoinedAt datetime2 NOT NULL,
    ImageUrl nvarchar(300) NULL
);
CREATE UNIQUE INDEX IX_Employees_EmpCode ON dbo.Employees(EmpCode);
CREATE INDEX IX_Employees_Department ON dbo.Employees(Department);
CREATE INDEX IX_Employees_UserId ON dbo.Employees(UserId);

CREATE TABLE dbo.Vendors (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(200) NOT NULL,
    ContactPerson nvarchar(200) NOT NULL,
    Email nvarchar(200) NOT NULL,
    Phone nvarchar(50) NOT NULL,
    Address nvarchar(400) NOT NULL,
    Products nvarchar(400) NOT NULL,
    ImageUrl nvarchar(300) NULL
);

CREATE TABLE dbo.Equipment (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(200) NOT NULL,
    Type nvarchar(30) NOT NULL,
    VendorId int NULL REFERENCES dbo.Vendors(Id),
    UnitPrice decimal(12,2) NOT NULL,
    Stock int NOT NULL,
    ReorderLevel int NOT NULL,
    ImageUrl nvarchar(300) NULL
);

CREATE TABLE dbo.Plans (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ConnectionType nvarchar(20) NOT NULL,
    Category nvarchar(100) NOT NULL,
    Name nvarchar(100) NOT NULL,
    Speed nvarchar(50) NULL,
    Price decimal(12,2) NOT NULL,
    ValidityLabel nvarchar(50) NOT NULL,
    ValidityMonths int NOT NULL,
    Deposit decimal(12,2) NOT NULL,
    CallRates nvarchar(400) NULL,
    Description nvarchar(1000) NOT NULL,
    Active bit NOT NULL,
    ImageUrl nvarchar(300) NULL
);
CREATE INDEX IX_Plans_ConnectionType ON dbo.Plans(ConnectionType);

CREATE TABLE dbo.Customers (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId int NULL REFERENCES dbo.Users(Id),
    Name nvarchar(200) NOT NULL,
    Email nvarchar(200) NULL,
    Phone nvarchar(50) NOT NULL,
    Address nvarchar(400) NOT NULL,
    City nvarchar(100) NOT NULL,
    CityCode nvarchar(10) NOT NULL,
    IdProof nvarchar(200) NULL,
    AltPhone nvarchar(50) NULL,
    CreatedAt datetime2 NOT NULL,
    ImageUrl nvarchar(300) NULL
);
CREATE INDEX IX_Customers_Phone ON dbo.Customers(Phone);
CREATE INDEX IX_Customers_UserId ON dbo.Customers(UserId);

CREATE TABLE dbo.Orders (
    Id int IDENTITY(1,1) PRIMARY KEY,
    OrderNo nvarchar(20) NOT NULL,
    CustomerId int NOT NULL REFERENCES dbo.Customers(Id),
    ConnectionType nvarchar(20) NOT NULL,
    PlanId int NULL REFERENCES dbo.Plans(Id),
    Quantity int NOT NULL,
    Corporate bit NOT NULL,
    DiscountPct int NOT NULL,
    Status nvarchar(20) NOT NULL,
    Remark nvarchar(1000) NULL,
    ShopId int NULL REFERENCES dbo.RetailShops(Id),
    CreatedBy nvarchar(200) NULL,
    CreatedAt datetime2 NOT NULL
);
CREATE UNIQUE INDEX IX_Orders_OrderNo ON dbo.Orders(OrderNo);
CREATE INDEX IX_Orders_Status ON dbo.Orders(Status);
CREATE INDEX IX_Orders_CreatedAt ON dbo.Orders(CreatedAt);
CREATE INDEX IX_Orders_CustomerId ON dbo.Orders(CustomerId);
CREATE INDEX IX_Orders_ShopId ON dbo.Orders(ShopId);

CREATE TABLE dbo.Connections (
    Id int IDENTITY(1,1) PRIMARY KEY,
    AccountId nvarchar(30) NOT NULL,
    OrderId int NULL REFERENCES dbo.Orders(Id),
    CustomerId int NOT NULL REFERENCES dbo.Customers(Id),
    PlanId int NULL REFERENCES dbo.Plans(Id),
    ConnectionType nvarchar(20) NOT NULL,
    City nvarchar(100) NOT NULL,
    CityCode nvarchar(10) NOT NULL,
    EquipmentId int NULL REFERENCES dbo.Equipment(Id),
    Status nvarchar(30) NOT NULL,
    ActivatedAt datetime2 NOT NULL,
    CreatedAt datetime2 NOT NULL
);
CREATE UNIQUE INDEX IX_Connections_AccountId ON dbo.Connections(AccountId);
CREATE INDEX IX_Connections_Status ON dbo.Connections(Status);
CREATE INDEX IX_Connections_CustomerId ON dbo.Connections(CustomerId);
CREATE INDEX IX_Connections_OrderId ON dbo.Connections(OrderId);

CREATE TABLE dbo.Bills (
    Id int IDENTITY(1,1) PRIMARY KEY,
    BillNo nvarchar(30) NOT NULL,
    ConnectionId int NOT NULL REFERENCES dbo.Connections(Id),
    Period nvarchar(50) NOT NULL,
    Rental decimal(12,2) NOT NULL,
    Usage decimal(12,2) NOT NULL,
    Tax decimal(12,2) NOT NULL,
    Discount decimal(12,2) NOT NULL,
    Total decimal(12,2) NOT NULL,
    PaidAmount decimal(12,2) NOT NULL,
    DueAmount decimal(12,2) NOT NULL,
    Status nvarchar(20) NOT NULL,
    DueDate datetime2 NULL,
    GeneratedBy nvarchar(200) NULL,
    CreatedAt datetime2 NOT NULL,
    RowVersion rowversion NULL
);
CREATE UNIQUE INDEX IX_Bills_BillNo ON dbo.Bills(BillNo);
CREATE INDEX IX_Bills_Status ON dbo.Bills(Status);
CREATE INDEX IX_Bills_ConnectionId ON dbo.Bills(ConnectionId);

CREATE TABLE dbo.Payments (
    Id int IDENTITY(1,1) PRIMARY KEY,
    BillId int NOT NULL REFERENCES dbo.Bills(Id),
    Amount decimal(12,2) NOT NULL,
    Method nvarchar(20) NOT NULL,
    Reference nvarchar(50) NULL,
    Channel nvarchar(20) NOT NULL,
    ReceivedBy nvarchar(200) NULL,
    CreatedAt datetime2 NOT NULL
);
CREATE INDEX IX_Payments_BillId ON dbo.Payments(BillId);

CREATE TABLE dbo.Feedback (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(200) NOT NULL,
    Contact nvarchar(200) NULL,
    ConnectionType nvarchar(20) NULL,
    Rating int NOT NULL,
    Message nvarchar(4000) NOT NULL,
    CreatedAt datetime2 NOT NULL
);
GO

/* ---------------------------------------------------------------------------
   UPGRADE for databases created BEFORE the picture feature (run once, safe to re-run).
   The app also does this automatically on start-up when Database:AutoCreate = true.
   --------------------------------------------------------------------------- */
IF COL_LENGTH('dbo.RetailShops', 'ImageUrl') IS NULL ALTER TABLE dbo.RetailShops ADD ImageUrl nvarchar(300) NULL;
IF COL_LENGTH('dbo.Employees',   'ImageUrl') IS NULL ALTER TABLE dbo.Employees   ADD ImageUrl nvarchar(300) NULL;
IF COL_LENGTH('dbo.Vendors',     'ImageUrl') IS NULL ALTER TABLE dbo.Vendors     ADD ImageUrl nvarchar(300) NULL;
IF COL_LENGTH('dbo.Equipment',   'ImageUrl') IS NULL ALTER TABLE dbo.Equipment   ADD ImageUrl nvarchar(300) NULL;
IF COL_LENGTH('dbo.Plans',       'ImageUrl') IS NULL ALTER TABLE dbo.Plans       ADD ImageUrl nvarchar(300) NULL;
IF COL_LENGTH('dbo.Customers',   'ImageUrl') IS NULL ALTER TABLE dbo.Customers   ADD ImageUrl nvarchar(300) NULL;
GO

/* ---------------------------------------------------------------------------
   Notifications (bell) + Audit log (admin "Activity log" tab).
   The app creates these automatically on start-up when Database:AutoCreate = true;
   run this script only if you manage the schema by hand (safe to re-run).
   --------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications (
        Id int IDENTITY(1,1) PRIMARY KEY,
        UserId int NOT NULL,                 -- the one person who sees this alert (no FK on purpose)
        [Type] nvarchar(30) NOT NULL,        -- order | bill | payment | complaint | ticket | info
        Title nvarchar(200) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        Link nvarchar(300) NULL,
        IsRead bit NOT NULL,
        CreatedAt datetime2 NOT NULL
    );
    CREATE INDEX IX_Notifications_UserId_IsRead ON dbo.Notifications(UserId, IsRead);
    CREATE INDEX IX_Notifications_CreatedAt ON dbo.Notifications(CreatedAt);
END
GO
IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs (
        Id int IDENTITY(1,1) PRIMARY KEY,
        UserId int NULL,
        UserName nvarchar(200) NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        [Action] nvarchar(30) NOT NULL,      -- create | update | delete | status | payment | image | login | logout
        Entity nvarchar(50) NOT NULL,
        EntityId nvarchar(50) NULL,
        Summary nvarchar(1500) NOT NULL,
        IpAddress nvarchar(45) NULL,
        CreatedAt datetime2 NOT NULL
    );
    CREATE INDEX IX_AuditLogs_CreatedAt ON dbo.AuditLogs(CreatedAt);
    CREATE INDEX IX_AuditLogs_UserId ON dbo.AuditLogs(UserId);
    CREATE INDEX IX_AuditLogs_Action ON dbo.AuditLogs(Action);
END
GO

-- ---------------------------------------------------------------------------
-- Complaint / ticket system
-- Status flow: open -> assigned -> in_progress -> resolved -> closed
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Tickets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tickets (
        Id int IDENTITY(1,1) PRIMARY KEY,
        TicketNo nvarchar(20) NOT NULL,                 -- TK-000001
        CustomerId int NOT NULL,
        Category nvarchar(60) NOT NULL,
        Subject nvarchar(200) NOT NULL,
        Description nvarchar(4000) NOT NULL,
        ImageUrl nvarchar(300) NULL,                    -- optional photo of the problem (/uploads/tickets/...)
        [Status] nvarchar(20) NOT NULL,                 -- open | assigned | in_progress | resolved | closed
        AssignedEmployeeId int NULL,                    -- technical-desk employee
        AssignedAt datetime2 NULL,
        ResolvedAt datetime2 NULL,
        ClosedAt datetime2 NULL,
        CreatedAt datetime2 NOT NULL,
        UpdatedAt datetime2 NOT NULL,
        CONSTRAINT FK_Tickets_Customers_CustomerId FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id),
        CONSTRAINT FK_Tickets_Employees_AssignedEmployeeId FOREIGN KEY (AssignedEmployeeId) REFERENCES dbo.Employees(Id)
    );
    CREATE UNIQUE INDEX IX_Tickets_TicketNo ON dbo.Tickets(TicketNo);
    CREATE INDEX IX_Tickets_Status ON dbo.Tickets([Status]);
    CREATE INDEX IX_Tickets_CustomerId ON dbo.Tickets(CustomerId);
    CREATE INDEX IX_Tickets_AssignedEmployeeId ON dbo.Tickets(AssignedEmployeeId);
END
GO
IF OBJECT_ID(N'dbo.TicketReplies', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TicketReplies (
        Id int IDENTITY(1,1) PRIMARY KEY,
        TicketId int NOT NULL,
        AuthorUserId int NULL,
        AuthorName nvarchar(200) NOT NULL,
        AuthorRole nvarchar(20) NOT NULL,
        [Message] nvarchar(2000) NOT NULL,
        ImageUrl nvarchar(300) NULL,
        IsSystem bit NOT NULL,                          -- automatic timeline line (assigned / resolved ...)
        CreatedAt datetime2 NOT NULL,
        CONSTRAINT FK_TicketReplies_Tickets_TicketId FOREIGN KEY (TicketId) REFERENCES dbo.Tickets(Id)
    );
    CREATE INDEX IX_TicketReplies_TicketId ON dbo.TicketReplies(TicketId);
END
GO

-- ---------------------------------------------------------------------------
-- Demo online payment (card / JazzCash / Easypaisa simulation). No card or wallet data is stored.
-- A successful attempt also adds a normal row to dbo.Payments (Channel = 'online', Reference = TransactionId).
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.OnlinePayments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OnlinePayments (
        Id int IDENTITY(1,1) PRIMARY KEY,
        TransactionId nvarchar(40) NOT NULL,            -- DEMO-20261014-A1B2C3
        BillId int NOT NULL,
        CustomerId int NOT NULL,
        Method nvarchar(20) NOT NULL,                   -- card | jazzcash | easypaisa
        Amount decimal(12,2) NOT NULL,
        [Status] nvarchar(10) NOT NULL,                 -- success | failed
        FailureReason nvarchar(200) NULL,
        PaymentId int NULL,                             -- the dbo.Payments row created on success
        CreatedAt datetime2 NOT NULL,
        CONSTRAINT FK_OnlinePayments_Bills_BillId FOREIGN KEY (BillId) REFERENCES dbo.Bills(Id),
        CONSTRAINT FK_OnlinePayments_Customers_CustomerId FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id),
        CONSTRAINT FK_OnlinePayments_Payments_PaymentId FOREIGN KEY (PaymentId) REFERENCES dbo.Payments(Id)
    );
    CREATE UNIQUE INDEX IX_OnlinePayments_TransactionId ON dbo.OnlinePayments(TransactionId);
    CREATE INDEX IX_OnlinePayments_BillId ON dbo.OnlinePayments(BillId);
    CREATE INDEX IX_OnlinePayments_CustomerId ON dbo.OnlinePayments(CustomerId);
END
GO
