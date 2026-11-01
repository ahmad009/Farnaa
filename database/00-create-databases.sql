USE [master];
GO
IF DB_ID(N'CatalogDb') IS NULL
    CREATE DATABASE [CatalogDb];
GO
IF DB_ID(N'OrderingDb') IS NULL
    CREATE DATABASE [OrderingDb];
GO
