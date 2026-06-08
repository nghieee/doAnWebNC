IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [Categories] (
    [CategoryId] int NOT NULL IDENTITY,
    [CategoryName] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [ImageUrl] nvarchar(max) NULL,
    [ParentCategoryId] int NULL,
    [IsFeature] bit NOT NULL,
    [CategoryLevel] nvarchar(max) NULL,
    [ProductCount] int NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryId]),
    CONSTRAINT [FK_Categories_Categories_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [Categories] ([CategoryId])
);

CREATE TABLE [User] (
    [UserId] int NOT NULL IDENTITY,
    [Username] nvarchar(max) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [FullName] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [Address] nvarchar(max) NULL,
    [Role] nvarchar(max) NULL,
    [CreatedDate] datetime2 NULL,
    [IsActive] bit NULL,
    CONSTRAINT [PK_User] PRIMARY KEY ([UserId])
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Products] (
    [ProductId] int NOT NULL IDENTITY,
    [ProductName] nvarchar(max) NOT NULL,
    [Brand] nvarchar(max) NULL,
    [Price] decimal(18,2) NOT NULL,
    [Package] nvarchar(max) NULL,
    [CategoryId] int NULL,
    [Ingredients] nvarchar(max) NULL,
    [Uses] nvarchar(max) NULL,
    [Dosage] nvarchar(max) NULL,
    [TargetUsers] nvarchar(max) NULL,
    [Contraindications] nvarchar(max) NULL,
    [IsFeature] bit NOT NULL,
    [Origin] nvarchar(max) NULL,
    [StockQuantity] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IngredientUnit] nvarchar(max) NULL,
    [Slug] nvarchar(max) NULL,
    [SoldQuantity] int NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([ProductId]),
    CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([CategoryId])
);

CREATE TABLE [Orders] (
    [OrderId] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NULL,
    [OrderDate] datetime2 NULL,
    [TotalAmount] decimal(18,2) NULL,
    [Status] nvarchar(max) NULL,
    [ShippingAddress] nvarchar(max) NULL,
    [PaymentStatus] nvarchar(max) NULL,
    [UserId1] int NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([OrderId]),
    CONSTRAINT [FK_Orders_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Orders_User_UserId1] FOREIGN KEY ([UserId1]) REFERENCES [User] ([UserId])
);

CREATE TABLE [InventoryTransactions] (
    [TransactionId] int NOT NULL IDENTITY,
    [ProductId] int NULL,
    [QuantityChange] int NULL,
    [TransactionType] nvarchar(max) NULL,
    [TransactionDate] datetime2 NULL,
    CONSTRAINT [PK_InventoryTransactions] PRIMARY KEY ([TransactionId]),
    CONSTRAINT [FK_InventoryTransactions_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([ProductId])
);

CREATE TABLE [ProductImage] (
    [ProductImageId] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [ImageUrl] nvarchar(max) NOT NULL,
    [SortOrder] int NULL,
    [IsMain] bit NULL,
    CONSTRAINT [PK_ProductImage] PRIMARY KEY ([ProductImageId]),
    CONSTRAINT [FK_ProductImage_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([ProductId]) ON DELETE CASCADE
);

CREATE TABLE [Reviews] (
    [ReviewId] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [ProductId] int NULL,
    [Rating] int NULL,
    [Comment] nvarchar(max) NULL,
    [ReviewDate] datetime2 NULL,
    CONSTRAINT [PK_Reviews] PRIMARY KEY ([ReviewId]),
    CONSTRAINT [FK_Reviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([ProductId]),
    CONSTRAINT [FK_Reviews_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
);

CREATE TABLE [OrderItems] (
    [OrderItemId] int NOT NULL IDENTITY,
    [OrderId] int NULL,
    [ProductId] int NULL,
    [Quantity] int NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_OrderItems] PRIMARY KEY ([OrderItemId]),
    CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([OrderId]),
    CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([ProductId])
);

CREATE TABLE [Payments] (
    [PaymentId] int NOT NULL IDENTITY,
    [OrderId] int NULL,
    [PaymentMethod] nvarchar(max) NULL,
    [Amount] decimal(18,2) NULL,
    [PaymentDate] datetime2 NULL,
    [PaymentStatus] nvarchar(max) NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([PaymentId]),
    CONSTRAINT [FK_Payments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([OrderId])
);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

CREATE INDEX [IX_Categories_ParentCategoryId] ON [Categories] ([ParentCategoryId]);

CREATE INDEX [IX_InventoryTransactions_ProductId] ON [InventoryTransactions] ([ProductId]);

CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);

CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);

CREATE INDEX [IX_Orders_UserId] ON [Orders] ([UserId]);

CREATE INDEX [IX_Orders_UserId1] ON [Orders] ([UserId1]);

CREATE INDEX [IX_Payments_OrderId] ON [Payments] ([OrderId]);

CREATE INDEX [IX_ProductImage_ProductId] ON [ProductImage] ([ProductId]);

CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);

CREATE INDEX [IX_Reviews_ProductId] ON [Reviews] ([ProductId]);

CREATE INDEX [IX_Reviews_UserId] ON [Reviews] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250712024312_InitialCreate', N'9.0.7');

ALTER TABLE [Orders] DROP CONSTRAINT [FK_Orders_User_UserId1];

ALTER TABLE [Reviews] DROP CONSTRAINT [FK_Reviews_User_UserId];

DROP TABLE [User];

DROP INDEX [IX_Orders_UserId1] ON [Orders];

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'UserId1');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [Orders] DROP COLUMN [UserId1];

DROP INDEX [IX_Reviews_UserId] ON [Reviews];
DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Reviews]') AND [c].[name] = N'UserId');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Reviews] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Reviews] ALTER COLUMN [UserId] nvarchar(450) NULL;
CREATE INDEX [IX_Reviews_UserId] ON [Reviews] ([UserId]);

ALTER TABLE [Reviews] ADD CONSTRAINT [FK_Reviews_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250712024729_UpdateReviewUserToIdentity', N'9.0.7');

ALTER TABLE [Orders] ADD [FullName] nvarchar(max) NULL;

ALTER TABLE [Orders] ADD [Phone] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250719160256_AddFullNameAndPhoneToOrder', N'9.0.7');

CREATE TABLE [ChatMessages] (
    [Id] int NOT NULL IDENTITY,
    [SenderId] nvarchar(max) NULL,
    [ReceiverId] nvarchar(max) NULL,
    [Message] nvarchar(max) NOT NULL,
    [SentAt] datetime2 NOT NULL,
    [IsRead] bit NOT NULL,
    CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id])
);

CREATE TABLE [UserRankInfos] (
    [UserId] nvarchar(450) NOT NULL,
    [TotalSpent] decimal(18,2) NOT NULL,
    [Rank] nvarchar(max) NOT NULL,
    [LastRankMailSent] datetime2 NULL,
    [LastNotiMailSent] datetime2 NULL,
    CONSTRAINT [PK_UserRankInfos] PRIMARY KEY ([UserId])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250721134445_AddChatMessageTable', N'9.0.7');

ALTER TABLE [AspNetUsers] ADD [LastRankMailSent] datetime2 NULL;

ALTER TABLE [AspNetUsers] ADD [LastRankNotiSent] datetime2 NULL;

ALTER TABLE [AspNetUsers] ADD [UserRank] nvarchar(max) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250723072642_AddUserRankAndMailStatusToUser', N'9.0.7');

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'LastRankMailSent');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [AspNetUsers] DROP COLUMN [LastRankMailSent];

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'LastRankNotiSent');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [AspNetUsers] DROP COLUMN [LastRankNotiSent];

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'UserRank');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [AspNetUsers] DROP COLUMN [UserRank];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250723074354_SyncUserRankInfo', N'9.0.7');

CREATE TABLE [Vouchers] (
    [VoucherId] int NOT NULL IDENTITY,
    [Code] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [ExpiryDate] datetime2 NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Vouchers] PRIMARY KEY ([VoucherId])
);

CREATE TABLE [UserVouchers] (
    [UserVoucherId] int NOT NULL IDENTITY,
    [UserId] nvarchar(max) NOT NULL,
    [VoucherId] int NOT NULL,
    [IsUsed] bit NOT NULL,
    [UsedDate] datetime2 NULL,
    CONSTRAINT [PK_UserVouchers] PRIMARY KEY ([UserVoucherId]),
    CONSTRAINT [FK_UserVouchers_Vouchers_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Vouchers] ([VoucherId]) ON DELETE CASCADE
);

CREATE INDEX [IX_UserVouchers_VoucherId] ON [UserVouchers] ([VoucherId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250723080606_AddVoucherAndUserVoucher', N'9.0.7');

ALTER TABLE [UserRankInfos] ADD [LastRankReset] datetime2 NULL;

ALTER TABLE [UserRankInfos] ADD [TotalSpent6Months] decimal(18,2) NOT NULL DEFAULT 0.0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250724005634_AddTotalSpent6MonthsAndLastRankResetToUserRankInfo', N'9.0.7');

ALTER TABLE [Vouchers] ADD [CategoryId] int NULL;

ALTER TABLE [Vouchers] ADD [CategoryName] nvarchar(max) NULL;

ALTER TABLE [Vouchers] ADD [Detail] nvarchar(max) NULL;

ALTER TABLE [Vouchers] ADD [DiscountType] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Vouchers] ADD [PercentValue] decimal(18,2) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250724070153_UpdateVoucherForPercentAndCategory', N'9.0.7');

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Vouchers]') AND [c].[name] = N'DiscountAmount');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Vouchers] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [Vouchers] ALTER COLUMN [DiscountAmount] decimal(18,2) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250724073014_MakeDiscountAmountNullableInVoucher', N'9.0.7');

ALTER TABLE [Orders] ADD [VoucherCode] nvarchar(max) NULL;

ALTER TABLE [Orders] ADD [VoucherDiscount] decimal(18,2) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250724141037_AddVoucherToOrder', N'9.0.7');

ALTER TABLE [UserVouchers] ADD [IsNew] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250725072718_AddIsNewToUserVoucher', N'9.0.7');

ALTER TABLE [Vouchers] ADD [MaxUsage] int NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250725140908_AddMaxUsageToVoucher', N'9.0.7');

ALTER TABLE [Vouchers] ADD [UsedCount] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250725142219_AddUsedCountToVoucher', N'9.0.7');

CREATE TABLE [Banners] (
    [BannerId] int NOT NULL IDENTITY,
    [Title] nvarchar(100) NOT NULL,
    [Description] nvarchar(200) NULL,
    [ImageUrl] nvarchar(max) NOT NULL,
    [LinkUrl] nvarchar(200) NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Banners] PRIMARY KEY ([BannerId])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250731144312_AddBannerTable', N'9.0.7');

ALTER TABLE [Banners] ADD [BannerType] nvarchar(max) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250731170937_AddBannerTypeToBanner', N'9.0.7');

ALTER TABLE [Payments] ADD [TransactionId] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250804171002_AddTransactionIdToPayment', N'9.0.7');

COMMIT;
GO

