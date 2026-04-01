USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'StockFlowDB')
BEGIN
    ALTER DATABASE StockFlowDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE StockFlowDB;
END
GO

CREATE DATABASE StockFlowDB;
GO

USE StockFlowDB;
GO

CREATE TABLE Users (
    UserId       INT IDENTITY(1,1) PRIMARY KEY,
    FullName     NVARCHAR(100)  NOT NULL,
    Email        NVARCHAR(150)  NOT NULL,
    PasswordHash NVARCHAR(500)  NOT NULL,
    Role         NVARCHAR(20)   NOT NULL DEFAULT 'Staff',
    CreatedAt    DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CHK_Users_Role CHECK (Role IN ('Admin', 'Manager', 'Staff'))
);
GO

CREATE TABLE Items (
    ItemId    INT IDENTITY(1,1) PRIMARY KEY,
    ItemName  NVARCHAR(150) NOT NULL,
    SKU       NVARCHAR(50)  NOT NULL,
    Unit      NVARCHAR(20)  NOT NULL DEFAULT 'kg',
    IsActive  BIT           NOT NULL DEFAULT 1,
    CreatedAt DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy INT           NOT NULL,
    CONSTRAINT UQ_Items_SKU    UNIQUE (SKU),
    CONSTRAINT FK_Items_Users  FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);
GO

CREATE TABLE Shipments (
    ShipmentId  INT IDENTITY(1,1) PRIMARY KEY,
    ItemId      INT            NOT NULL,
    TotalWeight FLOAT          NOT NULL,
    Status      NVARCHAR(20)   NOT NULL DEFAULT 'Pending',
    ReceivedAt  DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    ReceivedBy  INT            NOT NULL,
    CONSTRAINT FK_Shipments_Items      FOREIGN KEY (ItemId)     REFERENCES Items(ItemId),
    CONSTRAINT FK_Shipments_Users      FOREIGN KEY (ReceivedBy) REFERENCES Users(UserId),
    CONSTRAINT CHK_Shipments_Weight    CHECK (TotalWeight > 0),
    CONSTRAINT CHK_Shipments_Status    CHECK (Status IN ('Pending', 'InProgress', 'Processed'))
);
GO

CREATE TABLE ProcessedItems (
    ProcessedItemId INT IDENTITY(1,1) PRIMARY KEY,
    ParentId        INT           NULL,
    ItemId          INT           NOT NULL,
    ShipmentId      INT           NOT NULL,
    InputWeight     FLOAT         NOT NULL,
    OutputWeight    FLOAT         NOT NULL,
    Status          NVARCHAR(20)  NOT NULL DEFAULT 'Pending',
    ProcessedAt     DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    ProcessedBy     INT           NOT NULL,
    CONSTRAINT FK_ProcessedItems_Parent    FOREIGN KEY (ParentId)    REFERENCES ProcessedItems(ProcessedItemId),
    CONSTRAINT FK_ProcessedItems_Items     FOREIGN KEY (ItemId)      REFERENCES Items(ItemId),
    CONSTRAINT FK_ProcessedItems_Shipments FOREIGN KEY (ShipmentId)  REFERENCES Shipments(ShipmentId),
    CONSTRAINT FK_ProcessedItems_Users     FOREIGN KEY (ProcessedBy) REFERENCES Users(UserId),
    CONSTRAINT CHK_ProcessedItems_Input    CHECK (InputWeight > 0),
    CONSTRAINT CHK_ProcessedItems_Output   CHECK (OutputWeight > 0),
    CONSTRAINT CHK_ProcessedItems_Status   CHECK (Status IN ('Pending', 'Approved', 'Rejected'))
);
GO

CREATE TABLE AuditLogs (
    AuditLogId   INT IDENTITY(1,1) PRIMARY KEY,
    EntityName   NVARCHAR(100) NOT NULL,
    EntityId     INT           NOT NULL,
    Action       NVARCHAR(50)  NOT NULL,
    PerformedBy  INT           NOT NULL,
    Details      NVARCHAR(500) NULL,
    PerformedAt  DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (PerformedBy) REFERENCES Users(UserId)
);
GO

CREATE INDEX IX_Items_SKU
    ON Items(SKU);
GO

CREATE INDEX IX_Shipments_Status
    ON Shipments(Status);
GO

CREATE INDEX IX_Shipments_ReceivedAt
    ON Shipments(ReceivedAt);
GO

CREATE INDEX IX_ProcessedItems_ShipmentId
    ON ProcessedItems(ShipmentId);
GO

CREATE INDEX IX_ProcessedItems_ParentId
    ON ProcessedItems(ParentId);
GO

CREATE INDEX IX_ProcessedItems_Status
    ON ProcessedItems(Status);
GO

CREATE INDEX IX_AuditLogs_Entity
    ON AuditLogs(EntityName, EntityId);
GO

CREATE INDEX IX_AuditLogs_PerformedBy
    ON AuditLogs(PerformedBy);
GO

CREATE INDEX IX_AuditLogs_PerformedAt
    ON AuditLogs(PerformedAt DESC);
GO

DECLARE @AdminId INT;
DECLARE @ManagerId INT;
DECLARE @StaffId INT;

INSERT INTO Users (FullName, Email, PasswordHash, Role, CreatedAt)
VALUES
(
    'System Admin',
    'admin@stockflow.com',
    'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=',
    'Admin',
    GETUTCDATE()
),
(
    'Warehouse Manager',
    'manager@stockflow.com',
    'FHOSp1/lSZXCO2UOVSFrzQ8M5x3a7ZgE/4LbCsT7oa8=',
    'Manager',
    GETUTCDATE()
),
(
    'Warehouse Staff',
    'staff@stockflow.com',
    'XohImNooBHFR0OVvjcYpJ3NgPQ1qq73WKhHvch0VQtg=',
    'Staff',
    GETUTCDATE()
);
GO

SET @AdminId   = (SELECT UserId FROM Users WHERE Email = 'admin@stockflow.com');
SET @ManagerId = (SELECT UserId FROM Users WHERE Email = 'manager@stockflow.com');
SET @StaffId   = (SELECT UserId FROM Users WHERE Email = 'staff@stockflow.com');

INSERT INTO Items (ItemName, SKU, Unit, IsActive, CreatedAt, CreatedBy)
VALUES
('Basmati Rice',      'RICE-001', 'kg',  1, GETUTCDATE(), @AdminId),
('Wheat Flour',       'WFL-004',  'kg',  1, GETUTCDATE(), @AdminId),
('Refined Sugar',     'SUG-002',  'kg',  1, GETUTCDATE(), @AdminId),
('Iodised Salt',      'SLT-009',  'kg',  1, GETUTCDATE(), @AdminId),
('Corn Starch',       'CRN-007',  'kg',  1, GETUTCDATE(), @AdminId),
('Sunflower Oil',     'OIL-003',  'ltr', 1, GETUTCDATE(), @AdminId),
('Black Pepper',      'PEP-011',  'g',   1, GETUTCDATE(), @AdminId),
('Turmeric Powder',   'TRM-005',  'g',   1, GETUTCDATE(), @AdminId),
('Packaging Box 5kg', 'PKG-5KG',  'pcs', 1, GETUTCDATE(), @AdminId),
('Packaging Box 2kg', 'PKG-2KG',  'pcs', 1, GETUTCDATE(), @AdminId);
GO

DECLARE @RiceId   INT = (SELECT ItemId FROM Items WHERE SKU = 'RICE-001');
DECLARE @FlourId  INT = (SELECT ItemId FROM Items WHERE SKU = 'WFL-004');
DECLARE @SugarId  INT = (SELECT ItemId FROM Items WHERE SKU = 'SUG-002');
DECLARE @SaltId   INT = (SELECT ItemId FROM Items WHERE SKU = 'SLT-009');
DECLARE @CornId   INT = (SELECT ItemId FROM Items WHERE SKU = 'CRN-007');
DECLARE @StaffId2 INT = (SELECT UserId FROM Users WHERE Email = 'staff@stockflow.com');

INSERT INTO Shipments (ItemId, TotalWeight, Status, ReceivedAt, ReceivedBy)
VALUES
(@RiceId,  500,  'Pending',    DATEADD(DAY, -1,  GETUTCDATE()), @StaffId2),
(@FlourId, 1200, 'InProgress', DATEADD(DAY, -2,  GETUTCDATE()), @StaffId2),
(@SugarId, 800,  'Pending',    DATEADD(DAY, -3,  GETUTCDATE()), @StaffId2),
(@SaltId,  2000, 'Processed',  DATEADD(DAY, -5,  GETUTCDATE()), @StaffId2),
(@CornId,  400,  'Pending',    DATEADD(DAY, -26, GETUTCDATE()), @StaffId2);
GO

DECLARE @SaltShipmentId INT = (
    SELECT TOP 1 s.ShipmentId
    FROM Shipments s
    JOIN Items i ON s.ItemId = i.ItemId
    WHERE i.SKU = 'SLT-009'
);

DECLARE @Pkg5Id     INT = (SELECT ItemId FROM Items WHERE SKU = 'PKG-5KG');
DECLARE @Pkg2Id     INT = (SELECT ItemId FROM Items WHERE SKU = 'PKG-2KG');
DECLARE @StaffId3   INT = (SELECT UserId FROM Users WHERE Email = 'staff@stockflow.com');
DECLARE @ManagerId2 INT = (SELECT UserId FROM Users WHERE Email = 'manager@stockflow.com');

INSERT INTO ProcessedItems (ParentId, ItemId, ShipmentId, InputWeight, OutputWeight, Status, ProcessedAt, ProcessedBy)
VALUES
(NULL, @Pkg5Id, @SaltShipmentId, 2000, 1000, 'Approved', DATEADD(DAY, -4, GETUTCDATE()), @StaffId3),
(NULL, @Pkg2Id, @SaltShipmentId, 2000, 800,  'Approved', DATEADD(DAY, -4, GETUTCDATE()), @StaffId3);
GO

DECLARE @Child1Id INT = (
    SELECT TOP 1 ProcessedItemId
    FROM ProcessedItems
    WHERE ParentId IS NULL AND OutputWeight = 1000
);

DECLARE @Child2Id INT = (
    SELECT TOP 1 ProcessedItemId
    FROM ProcessedItems
    WHERE ParentId IS NULL AND OutputWeight = 800
);

DECLARE @Pkg5Id2   INT = (SELECT ItemId FROM Items WHERE SKU = 'PKG-5KG');
DECLARE @Pkg2Id2   INT = (SELECT ItemId FROM Items WHERE SKU = 'PKG-2KG');
DECLARE @StaffId4  INT = (SELECT UserId FROM Users WHERE Email = 'staff@stockflow.com');
DECLARE @SaltShipId2 INT = (
    SELECT TOP 1 s.ShipmentId
    FROM Shipments s
    JOIN Items i ON s.ItemId = i.ItemId
    WHERE i.SKU = 'SLT-009'
);

INSERT INTO ProcessedItems (ParentId, ItemId, ShipmentId, InputWeight, OutputWeight, Status, ProcessedAt, ProcessedBy)
VALUES
(@Child1Id, @Pkg5Id2, @SaltShipId2, 1000, 400, 'Approved', DATEADD(DAY, -3, GETUTCDATE()), @StaffId4),
(@Child1Id, @Pkg2Id2, @SaltShipId2, 1000, 500, 'Pending',  DATEADD(DAY, -3, GETUTCDATE()), @StaffId4);
GO

DECLARE @ManagerId3 INT = (SELECT UserId FROM Users WHERE Email = 'manager@stockflow.com');
DECLARE @StaffId5   INT = (SELECT UserId FROM Users WHERE Email = 'staff@stockflow.com');

INSERT INTO AuditLogs (EntityName, EntityId, Action, PerformedBy, Details, PerformedAt)
SELECT
    'Shipment',
    ShipmentId,
    'Receive',
    @StaffId5,
    'Seeded shipment',
    ReceivedAt
FROM Shipments;

INSERT INTO AuditLogs (EntityName, EntityId, Action, PerformedBy, Details, PerformedAt)
SELECT
    'ProcessedItem',
    ProcessedItemId,
    'Process',
    ProcessedBy,
    'Seeded processed item',
    ProcessedAt
FROM ProcessedItems;

INSERT INTO AuditLogs (EntityName, EntityId, Action, PerformedBy, Details, PerformedAt)
SELECT
    'ProcessedItem',
    ProcessedItemId,
    'Approve',
    @ManagerId3,
    'Seeded approval',
    DATEADD(HOUR, 1, ProcessedAt)
FROM ProcessedItems
WHERE Status = 'Approved';
GO

SELECT 'Users'          AS TableName, COUNT(*) AS Rows FROM Users
UNION ALL
SELECT 'Items',          COUNT(*) FROM Items
UNION ALL
SELECT 'Shipments',      COUNT(*) FROM Shipments
UNION ALL
SELECT 'ProcessedItems', COUNT(*) FROM ProcessedItems
UNION ALL
SELECT 'AuditLogs',      COUNT(*) FROM AuditLogs;
GO