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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260426201243_InitialCreate'
)
BEGIN
    CREATE TABLE [MangaCollectionItems] (
        [Id] uniqueidentifier NOT NULL,
        [MalId] int NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [ImageUrl] nvarchar(1000) NULL,
        [MalTotalVolumes] int NULL,
        [CustomTotalVolumes] int NULL,
        CONSTRAINT [PK_MangaCollectionItems] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260426201243_InitialCreate'
)
BEGIN
    CREATE TABLE [OwnedVolumes] (
        [Id] uniqueidentifier NOT NULL,
        [VolumeNumber] int NOT NULL,
        [PurchaseDate] date NULL,
        [Price] decimal(10,2) NULL,
        [Store] nvarchar(200) NULL,
        [MangaCollectionItemId] uniqueidentifier NULL,
        CONSTRAINT [PK_OwnedVolumes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OwnedVolumes_MangaCollectionItems_MangaCollectionItemId] FOREIGN KEY ([MangaCollectionItemId]) REFERENCES [MangaCollectionItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260426201243_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MangaCollectionItems_MalId] ON [MangaCollectionItems] ([MalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260426201243_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OwnedVolumes_MangaCollectionItemId] ON [OwnedVolumes] ([MangaCollectionItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260426201243_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260426201243_InitialCreate', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427104540_AddUserIdToCollectionItems'
)
BEGIN
    DROP INDEX [IX_MangaCollectionItems_MalId] ON [MangaCollectionItems];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427104540_AddUserIdToCollectionItems'
)
BEGIN
    ALTER TABLE [MangaCollectionItems] ADD [UserId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427104540_AddUserIdToCollectionItems'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MangaCollectionItems_UserId_MalId] ON [MangaCollectionItems] ([UserId], [MalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427104540_AddUserIdToCollectionItems'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260427104540_AddUserIdToCollectionItems', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428071256_AddUsers'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [Email] nvarchar(320) NOT NULL,
        [PasswordHash] nvarchar(1000) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428071256_AddUsers'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428071256_AddUsers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260428071256_AddUsers', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428085512_AddUserForeignKeyToCollectionItems'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260428085512_AddUserForeignKeyToCollectionItems', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428090058_AddUserForeignKeyToCollectionItems2'
)
BEGIN
    ALTER TABLE [MangaCollectionItems] ADD CONSTRAINT [FK_MangaCollectionItems_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428090058_AddUserForeignKeyToCollectionItems2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260428090058_AddUserForeignKeyToCollectionItems2', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429080548_AddEmailConfirmationAndUserTokens'
)
BEGIN
    ALTER TABLE [Users] ADD [EmailConfirmedAt] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429080548_AddEmailConfirmationAndUserTokens'
)
BEGIN
    ALTER TABLE [Users] ADD [IsEmailConfirmed] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429080548_AddEmailConfirmationAndUserTokens'
)
BEGIN
    CREATE TABLE [UserTokens] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [TokenHash] nvarchar(500) NOT NULL,
        [Type] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [ExpiresAt] datetimeoffset NOT NULL,
        [UsedAt] datetimeoffset NULL,
        CONSTRAINT [PK_UserTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429080548_AddEmailConfirmationAndUserTokens'
)
BEGIN
    CREATE INDEX [IX_UserTokens_TokenHash_Type] ON [UserTokens] ([TokenHash], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429080548_AddEmailConfirmationAndUserTokens'
)
BEGIN
    CREATE INDEX [IX_UserTokens_UserId] ON [UserTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429080548_AddEmailConfirmationAndUserTokens'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429080548_AddEmailConfirmationAndUserTokens', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503204811_AddComicVineCatalogModel'
)
BEGIN
    CREATE TABLE [Series] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [OriginalTitle] nvarchar(300) NULL,
        [Description] nvarchar(4000) NULL,
        [ImageUrl] nvarchar(1000) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_Series] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503204811_AddComicVineCatalogModel'
)
BEGIN
    CREATE TABLE [Editions] (
        [Id] uniqueidentifier NOT NULL,
        [SeriesId] uniqueidentifier NOT NULL,
        [ComicVineVolumeId] int NOT NULL,
        [ComicVineApiDetailUrl] nvarchar(1000) NOT NULL,
        [Name] nvarchar(300) NOT NULL,
        [PublisherName] nvarchar(200) NULL,
        [StartYear] int NULL,
        [Description] nvarchar(4000) NULL,
        [ImageUrl] nvarchar(1000) NULL,
        [SiteDetailUrl] nvarchar(1000) NULL,
        [IssueCount] int NULL,
        [ImportedAt] datetimeoffset NOT NULL,
        [LastSyncedAt] datetimeoffset NULL,
        CONSTRAINT [PK_Editions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Editions_Series_SeriesId] FOREIGN KEY ([SeriesId]) REFERENCES [Series] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503204811_AddComicVineCatalogModel'
)
BEGIN
    CREATE TABLE [Tomes] (
        [Id] uniqueidentifier NOT NULL,
        [EditionId] uniqueidentifier NOT NULL,
        [ComicVineIssueId] int NOT NULL,
        [ComicVineApiDetailUrl] nvarchar(1000) NOT NULL,
        [IssueNumber] nvarchar(50) NOT NULL,
        [NormalizedNumber] int NULL,
        [Title] nvarchar(300) NULL,
        [ImageUrl] nvarchar(1000) NULL,
        [CoverDate] date NULL,
        [StoreDate] date NULL,
        [SiteDetailUrl] nvarchar(1000) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_Tomes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tomes_Editions_EditionId] FOREIGN KEY ([EditionId]) REFERENCES [Editions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503204811_AddComicVineCatalogModel'
)
BEGIN
    CREATE TABLE [UserCollections] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [EditionId] uniqueidentifier NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_UserCollections] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserCollections_Editions_EditionId] FOREIGN KEY ([EditionId]) REFERENCES [Editions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserCollections_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503204811_AddComicVineCatalogModel'
)
BEGIN
    CREATE TABLE [UserOwnedTomes] (
        [Id] uniqueidentifier NOT NULL,
        [UserCollectionId] uniqueidentifier NOT NULL,
        [TomeId] uniqueidentifier NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_UserOwnedTomes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserOwnedTomes_Tomes_TomeId] FOREIGN KEY ([TomeId]) REFERENCES [Tomes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserOwnedTomes_UserCollections_UserCollectionId] FOREIGN KEY ([UserCollectionId]) REFERENCES [UserCollections] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503204811_AddComicVineCatalogModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Editions_ComicVineApiDetailUrl] ON [Editions] ([ComicVineApiDetailUrl]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503204811_AddComicVineCatalogModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Editions_ComicVineVolumeId] ON [Editions] ([ComicVineVolumeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503204811_AddComicVineCatalogModel'
)
BEGIN
    CREATE INDEX [IX_Editions_SeriesId] ON [Editions] ([SeriesId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503204811_AddComicVineCatalogModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tomes_ComicVineApiDetailUrl] ON [Tomes] ([ComicVineApiDetailUrl]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503204811_AddComicVineCatalogModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tomes_ComicVineIssueId] ON [Tomes] ([ComicVineIssueId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503204811_AddComicVineCatalogModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tomes_EditionId_IssueNumber] ON [Tomes] ([EditionId], [IssueNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503204811_AddComicVineCatalogModel'
)
BEGIN
    CREATE INDEX [IX_UserCollections_EditionId] ON [UserCollections] ([EditionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503204811_AddComicVineCatalogModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserCollections_UserId_EditionId] ON [UserCollections] ([UserId], [EditionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503204811_AddComicVineCatalogModel'
)
BEGIN
    CREATE INDEX [IX_UserOwnedTomes_TomeId] ON [UserOwnedTomes] ([TomeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503204811_AddComicVineCatalogModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserOwnedTomes_UserCollectionId_TomeId] ON [UserOwnedTomes] ([UserCollectionId], [TomeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503204811_AddComicVineCatalogModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260503204811_AddComicVineCatalogModel', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506162314_RemoveLegacyMalCollectionModel'
)
BEGIN
    DROP TABLE [OwnedVolumes];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506162314_RemoveLegacyMalCollectionModel'
)
BEGIN
    DROP TABLE [MangaCollectionItems];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506162314_RemoveLegacyMalCollectionModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506162314_RemoveLegacyMalCollectionModel', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003175922_AddUserSecurityStamp'
)
BEGIN
    ALTER TABLE [Users] ADD [SecurityStamp] nvarchar(64) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003175922_AddUserSecurityStamp'
)
BEGIN
    EXEC('UPDATE [Users] SET [SecurityStamp] = LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), ''-'', ''''))')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003175922_AddUserSecurityStamp'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003175922_AddUserSecurityStamp', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003185701_AllowDuplicateIssueNumbersPerEdition'
)
BEGIN
    DROP INDEX [IX_Tomes_EditionId_IssueNumber] ON [Tomes];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003185701_AllowDuplicateIssueNumbersPerEdition'
)
BEGIN
    CREATE INDEX [IX_Tomes_EditionId_IssueNumber] ON [Tomes] ([EditionId], [IssueNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003185701_AllowDuplicateIssueNumbersPerEdition'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003185701_AllowDuplicateIssueNumbersPerEdition', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007134733_MakeUserTokenUsedAtConcurrencyToken'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261007134733_MakeUserTokenUsedAtConcurrencyToken', N'10.0.12');
END;

COMMIT;
GO

