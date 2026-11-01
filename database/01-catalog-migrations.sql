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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101145422_InitialCatalog'
)
BEGIN
    CREATE TABLE [dbo].[Products] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [SKU] nvarchar(64) NOT NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101145422_InitialCatalog'
)
BEGIN
    CREATE TABLE [dbo].[Reservations] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [Released] bit NOT NULL,
        CONSTRAINT [PK_Reservations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101145422_InitialCatalog'
)
BEGIN
    CREATE TABLE [dbo].[SalesSettings] (
        [Id] int NOT NULL,
        [MaxOrderItems] int NOT NULL,
        [MaxItemQuantity] int NOT NULL,
        [MaxUnitPrice] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_SalesSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SalesSettings_Limits] CHECK (MaxOrderItems > 0 AND MaxItemQuantity > 0 AND MaxUnitPrice >= 0),
        CONSTRAINT [CK_SalesSettings_SingleRow] CHECK (Id = 1)
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101145422_InitialCatalog'
)
BEGIN
    CREATE TABLE [dbo].[Sellers] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        CONSTRAINT [PK_Sellers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101145422_InitialCatalog'
)
BEGIN
    CREATE TABLE [dbo].[SellingOptions] (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [SellerId] uniqueidentifier NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [StockQuantity] int NOT NULL,
        CONSTRAINT [PK_SellingOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SellingOptions_Price] CHECK (Price >= 0),
        CONSTRAINT [CK_SellingOptions_Stock] CHECK (StockQuantity >= 0),
        CONSTRAINT [FK_SellingOptions_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SellingOptions_Sellers_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [dbo].[Sellers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101145422_InitialCatalog'
)
BEGIN
    CREATE TABLE [dbo].[ReservedStockItems] (
        [ReservationId] uniqueidentifier NOT NULL,
        [SellingOptionId] uniqueidentifier NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_ReservedStockItems] PRIMARY KEY ([ReservationId], [SellingOptionId]),
        CONSTRAINT [CK_ReservedStockItems_Price] CHECK (UnitPrice >= 0),
        CONSTRAINT [CK_ReservedStockItems_Quantity] CHECK (Quantity > 0),
        CONSTRAINT [FK_ReservedStockItems_Reservations_ReservationId] FOREIGN KEY ([ReservationId]) REFERENCES [dbo].[Reservations] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ReservedStockItems_SellingOptions_SellingOptionId] FOREIGN KEY ([SellingOptionId]) REFERENCES [dbo].[SellingOptions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101145422_InitialCatalog'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'SKU') AND [object_id] = OBJECT_ID(N'[dbo].[Products]'))
        SET IDENTITY_INSERT [dbo].[Products] ON;
    EXEC(N'INSERT INTO [dbo].[Products] ([Id], [Name], [SKU])
    VALUES (''10000000-0000-0000-0000-000000000001'', N''تیتاپ'', N''KB-001''),
    (''10000000-0000-0000-0000-000000000002'', N''رنگارنگ'', N''KB-002'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'SKU') AND [object_id] = OBJECT_ID(N'[dbo].[Products]'))
        SET IDENTITY_INSERT [dbo].[Products] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101145422_InitialCatalog'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'MaxItemQuantity', N'MaxOrderItems', N'MaxUnitPrice') AND [object_id] = OBJECT_ID(N'[dbo].[SalesSettings]'))
        SET IDENTITY_INSERT [dbo].[SalesSettings] ON;
    EXEC(N'INSERT INTO [dbo].[SalesSettings] ([Id], [MaxItemQuantity], [MaxOrderItems], [MaxUnitPrice])
    VALUES (1, 100000, 100, 1000000000.0)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'MaxItemQuantity', N'MaxOrderItems', N'MaxUnitPrice') AND [object_id] = OBJECT_ID(N'[dbo].[SalesSettings]'))
        SET IDENTITY_INSERT [dbo].[SalesSettings] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101145422_InitialCatalog'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Title') AND [object_id] = OBJECT_ID(N'[dbo].[Sellers]'))
        SET IDENTITY_INSERT [dbo].[Sellers] ON;
    EXEC(N'INSERT INTO [dbo].[Sellers] ([Id], [Title])
    VALUES (''20000000-0000-0000-0000-000000000001'', N''شهرام''),
    (''20000000-0000-0000-0000-000000000002'', N''بهرام'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Title') AND [object_id] = OBJECT_ID(N'[dbo].[Sellers]'))
        SET IDENTITY_INSERT [dbo].[Sellers] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101145422_InitialCatalog'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Price', N'ProductId', N'SellerId', N'StockQuantity') AND [object_id] = OBJECT_ID(N'[dbo].[SellingOptions]'))
        SET IDENTITY_INSERT [dbo].[SellingOptions] ON;
    EXEC(N'INSERT INTO [dbo].[SellingOptions] ([Id], [Price], [ProductId], [SellerId], [StockQuantity])
    VALUES (''30000000-0000-0000-0000-000000000001'', 125.5, ''10000000-0000-0000-0000-000000000001'', ''20000000-0000-0000-0000-000000000001'', 1),
    (''30000000-0000-0000-0000-000000000002'', 130.0, ''10000000-0000-0000-0000-000000000001'', ''20000000-0000-0000-0000-000000000002'', 10)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Price', N'ProductId', N'SellerId', N'StockQuantity') AND [object_id] = OBJECT_ID(N'[dbo].[SellingOptions]'))
        SET IDENTITY_INSERT [dbo].[SellingOptions] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101145422_InitialCatalog'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_SKU] ON [dbo].[Products] ([SKU]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101145422_InitialCatalog'
)
BEGIN
    CREATE INDEX [IX_Reservations_Released_CreatedAt] ON [dbo].[Reservations] ([Released], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101145422_InitialCatalog'
)
BEGIN
    CREATE INDEX [IX_ReservedStockItems_SellingOptionId] ON [dbo].[ReservedStockItems] ([SellingOptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101145422_InitialCatalog'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SellingOptions_ProductId_SellerId] ON [dbo].[SellingOptions] ([ProductId], [SellerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101145422_InitialCatalog'
)
BEGIN
    CREATE INDEX [IX_SellingOptions_SellerId] ON [dbo].[SellingOptions] ([SellerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101145422_InitialCatalog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101145422_InitialCatalog', N'8.0.33');
END;
GO

COMMIT;
GO

