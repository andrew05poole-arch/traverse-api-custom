DECLARE @dbName as nvarchar(128), @sql nvarchar(max)

CREATE TABLE #DatabaseList (DbName nvarchar(128))

DECLARE dbSearch CURSOR FOR
SELECT [name]
FROM [master].sys.databases
WHERE HAS_DBACCESS([name]) = 1

OPEN dbSearch

FETCH NEXT FROM dbSearch INTO @dbName

WHILE @@FETCH_STATUS = 0
BEGIN
	SET @sql = N'
DECLARE @innerSql nvarchar(max)

IF EXISTS (SELECT * FROM [' + @dbName + '].sys.tables WHERE [name] = ''ApiInfo'' AND [type] = ''U'')
BEGIN
	SET @innerSql = ''
	INSERT INTO #DatabaseList (DbName)
	SELECT ''''' + @dbName + '''''
	FROM [' + @dbName + ']..ApiInfo
	WHERE SysDb = ''''' + DB_NAME() + ''''' ''

	EXEC (@innerSql)
END'

	EXEC (@sql)
	FETCH NEXT FROM dbSearch INTO @dbName
END

CLOSE dbSearch
DEALLOCATE dbSearch

DECLARE dbCreate CURSOR FOR
SELECT DbName 
FROM #DatabaseList

OPEN dbCreate

FETCH NEXT FROM dbCreate INTO @dbName

WHILE @@FETCH_STATUS = 0
BEGIN
	SET @sql = N'
	USE [' + @dbName + N'];

IF NOT EXISTS (SELECT * FROM sys.tables WHERE [object_id] = OBJECT_ID(N''[dbo].[ApiFunctionHeader]''))
BEGIN
	CREATE TABLE [dbo].[ApiFunctionHeader] (
		[ID] [uniqueidentifier] NOT NULL,		--Generated GUID by creator of function
		[Name] [nvarchar](50) NULL,				--Function Name shown to on client setup screen
		[AppId] [nvarchar](4) NULL,				--Valid application listed in SysDB tblSmApp; used for grouping
		[Type] [tinyint] NULL,					--Enum (1 = Setup and Maintenance; 2 = Transactions; 3 = Other/Miscellaneous)
		[Notes] [nvarchar](max) NULL,			--Notes for documentation
		[Scope] [tinyint] NULL,					--Enum (1 = GET; 2 = POST; 4 = PUT; 8 = DELETE)
		[OverrideID] [uniqueidentifier] NULL,	--Used by custom functions to replace existing functions
		[QueryTableName] [nvarchar](max) NULL,
		[ts] [timestamp] NULL,
		CONSTRAINT [PK_ApiFunctionHeader] PRIMARY KEY CLUSTERED (
			[ID] ASC
		) ON [PRIMARY]
	) ON [PRIMARY];
END
ELSE
BEGIN
	ALTER TABLE [dbo].[ApiFunctionHeader] ALTER COLUMN [QueryTableName] [nvarchar](max) NULL;
END

	USE [' + DB_NAME() + '];
'

	EXEC (@sql)
	FETCH NEXT FROM dbCreate INTO @dbName
END

CLOSE dbCreate
DEALLOCATE dbCreate
DECLARE dbUpdate CURSOR FOR
SELECT DbName 
FROM #DatabaseList

OPEN dbUpdate

FETCH NEXT FROM dbUpdate INTO @dbName

WHILE @@FETCH_STATUS = 0
BEGIN
	SET @sql = N'
	USE [' + @dbName + N'];

DELETE FROM ApiFunctionHeader WHERE ID = N''765ae38b-af2c-4529-b445-a914ca369a4b''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''765ae38b-af2c-4529-b445-a914ca369a4b'' As ID, N''SO Transaction Order'' As [Name], N''SO'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''c16315f8-8f21-419d-90c1-3176c35dddc6''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As ID, N''SO Transaction Order Detail'' As [Name], N''SO'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''19e5ccc8-baeb-4dee-8110-5b566fb118d7''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''19e5ccc8-baeb-4dee-8110-5b566fb118d7'' As ID, N''SO Transaction Order Serial'' As [Name], N''SO'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''e59b69e9-fce8-47ad-bfec-c4bf934b6e63''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''e59b69e9-fce8-47ad-bfec-c4bf934b6e63'' As ID, N''SO Transaction Order Lot'' As [Name], N''SO'' As AppId, 2 As [Type], '''' As Notes, 15 As Scope, NULL As OverrideID


	USE [' + DB_NAME() + '];
'

	EXEC (@sql)
	FETCH NEXT FROM dbUpdate INTO @dbName
END

CLOSE dbUpdate
DEALLOCATE dbUpdate

DROP TABLE #DatabaseList;
