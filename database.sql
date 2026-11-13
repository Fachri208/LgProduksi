/* ============================================================
   Sistem Manajemen Produksi dan Penjualan Produk Elektronik
   PT LG Electronics Indonesia  -  SQL Server 2017+
   Jalankan seluruh skrip ini di SSMS. PERHATIAN: tabel lama akan dihapus.
   ============================================================ */
IF DB_ID(N'LgProduksiDB') IS NULL CREATE DATABASE LgProduksiDB;
GO
USE LgProduksiDB;
GO

IF OBJECT_ID('OrderItems') IS NOT NULL DROP TABLE OrderItems;
IF OBJECT_ID('Orders') IS NOT NULL DROP TABLE Orders;
IF OBJECT_ID('Cart') IS NOT NULL DROP TABLE Cart;
IF OBJECT_ID('Catalog') IS NOT NULL DROP TABLE Catalog;
IF OBJECT_ID('ProductComponents') IS NOT NULL DROP TABLE ProductComponents;
IF OBJECT_ID('Products') IS NOT NULL DROP TABLE Products;
IF OBJECT_ID('Components') IS NOT NULL DROP TABLE Components;
IF OBJECT_ID('Users') IS NOT NULL DROP TABLE Users;
GO

CREATE TABLE Users (
    UserId       INT IDENTITY(1,1) PRIMARY KEY,
    Username     VARCHAR(50)   NOT NULL UNIQUE,
    PasswordHash VARCHAR(64)   NOT NULL,              -- SHA-256 (hex)
    FullName     NVARCHAR(100) NOT NULL,
    Role         VARCHAR(20)   NOT NULL CHECK (Role IN ('Supplier','Sales','Customer')),
    Phone        VARCHAR(20)   NULL,
    Address      NVARCHAR(250) NULL,
    IsActive     BIT           NOT NULL DEFAULT 1
);

CREATE TABLE Components (
    ComponentId INT IDENTITY(1,1) PRIMARY KEY,
    Code        VARCHAR(30)   NOT NULL UNIQUE,
    Name        NVARCHAR(100) NOT NULL,
    Category    NVARCHAR(50)  NULL,
    Unit        VARCHAR(20)   NOT NULL DEFAULT 'pcs',
    Stock       INT           NOT NULL DEFAULT 0 CHECK (Stock >= 0),
    Price       DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (Price >= 0),
    IsActive    BIT           NOT NULL DEFAULT 1
);

CREATE TABLE Products (
    ProductId   INT IDENTITY(1,1) PRIMARY KEY,
    Code        VARCHAR(30)   NOT NULL UNIQUE,
    Name        NVARCHAR(100) NOT NULL,
    Category    NVARCHAR(50)  NULL,
    Description NVARCHAR(500) NULL,
    Stock       INT           NOT NULL DEFAULT 0 CHECK (Stock >= 0),
    CreatedBy   INT           NOT NULL REFERENCES Users(UserId),
    CreatedAt   DATETIME      NOT NULL DEFAULT GETDATE()
);

CREATE TABLE ProductComponents (             -- komposisi produk (BOM)
    ProductId   INT NOT NULL REFERENCES Products(ProductId) ON DELETE CASCADE,
    ComponentId INT NOT NULL REFERENCES Components(ComponentId),
    Qty         INT NOT NULL CHECK (Qty > 0),
    PRIMARY KEY (ProductId, ComponentId)
);

CREATE TABLE Catalog (                        -- katalog penjualan
    CatalogId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL UNIQUE REFERENCES Products(ProductId),
    SellPrice DECIMAL(18,2) NOT NULL CHECK (SellPrice >= 0),
    Stock     INT NOT NULL DEFAULT 0 CHECK (Stock >= 0),
    IsListed  BIT NOT NULL DEFAULT 1
);

CREATE TABLE Cart (
    CartId     INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL REFERENCES Users(UserId),
    CatalogId  INT NOT NULL REFERENCES Catalog(CatalogId),
    Qty        INT NOT NULL CHECK (Qty > 0),
    UNIQUE (CustomerId, CatalogId)
);

CREATE TABLE Orders (
    OrderId     INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId  INT NOT NULL REFERENCES Users(UserId),
    OrderDate   DATETIME NOT NULL DEFAULT GETDATE(),
    Total       DECIMAL(18,2) NOT NULL,
    Status      VARCHAR(20) NOT NULL DEFAULT 'Menunggu'
                CHECK (Status IN ('Menunggu','Diproses','Selesai','Dibatalkan')),
    ProcessedBy INT NULL REFERENCES Users(UserId)
);

CREATE TABLE OrderItems (
    OrderId   INT NOT NULL REFERENCES Orders(OrderId) ON DELETE CASCADE,
    CatalogId INT NOT NULL REFERENCES Catalog(CatalogId),
    Qty       INT NOT NULL CHECK (Qty > 0),
    Price     DECIMAL(18,2) NOT NULL,
    PRIMARY KEY (OrderId, CatalogId)
);
GO

/* ------------------- DATA CONTOH ------------------- */
INSERT INTO Users (Username, PasswordHash, FullName, Role, Phone, Address) VALUES
('supplier', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256','supplier123'), 2), N'Budi Supplier',   'Supplier', '081200000001', N'Cikarang, Bekasi'),
('sales',    CONVERT(VARCHAR(64), HASHBYTES('SHA2_256','sales123'),    2), N'Sari Sales',      'Sales',    '081200000002', N'Cikarang, Bekasi'),
('customer', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256','customer123'), 2), N'Andi Customer',   'Customer', '081200000003', N'Jl. Sudirman No. 10, Jakarta');

INSERT INTO Components (Code, Name, Category, Unit, Stock, Price) VALUES
('CMP-PNL55', N'Panel OLED 55 inci',  N'Panel',      'pcs', 40, 6500000),
('CMP-MBTV',  N'Mainboard TV',        N'Elektronik', 'pcs', 60, 1200000),
('CMP-PSU',   N'Power Supply',        N'Elektronik', 'pcs', 90,  350000),
('CMP-SPK',   N'Speaker',             N'Audio',      'pcs', 150, 120000),
('CMP-RMT',   N'Remote Control',      N'Aksesoris',  'pcs', 80,   90000),
('CMP-STD',   N'Stand / Kaki',        N'Aksesoris',  'pcs', 70,  150000),
('CMP-PNL27', N'Panel LCD 27 inci',   N'Panel',      'pcs', 50, 1800000),
('CMP-MBMN',  N'Mainboard Monitor',   N'Elektronik', 'pcs', 55,  600000),
('CMP-HDMI',  N'Kabel HDMI',          N'Aksesoris',  'pcs', 120,  40000);

INSERT INTO Products (Code, Name, Category, Description, Stock, CreatedBy) VALUES
('PRD-TV55', N'LG OLED TV 55 inci',       N'TV',      N'Smart TV OLED 4K 55 inci dengan webOS.', 10, 1),
('PRD-MN27', N'LG UltraGear Monitor 27',  N'Monitor', N'Monitor gaming 27 inci 144Hz.',          15, 1);

INSERT INTO ProductComponents (ProductId, ComponentId, Qty) VALUES
(1,1,1),(1,2,1),(1,3,1),(1,4,2),(1,5,1),(1,6,1),
(2,7,1),(2,8,1),(2,3,1),(2,9,1),(2,6,1);

INSERT INTO Catalog (ProductId, SellPrice, Stock, IsListed) VALUES (1, 12500000, 5, 1);
GO
