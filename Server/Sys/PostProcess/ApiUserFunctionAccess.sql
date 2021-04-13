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

DECLARE dbUpdate CURSOR FOR
SELECT DbName 
FROM #DatabaseList

OPEN dbUpdate

FETCH NEXT FROM dbUpdate INTO @dbName

WHILE @@FETCH_STATUS = 0
BEGIN
	SET @sql = N'
	USE [' + @dbName + N'];

IF NOT EXISTS (SELECT * FROM sys.tables WHERE [object_id] = OBJECT_ID(N''[dbo].[ApiUserFunctionAccess]''))
BEGIN
	CREATE TABLE [dbo].[ApiUserFunctionAccess] (
		[UserFunctionID] [bigint] NOT NULL,		--FK:[ApiUserFunction]
		[AccessCount] [bigint] NULL,
		[FirstAccessTime] [datetime] NULL,
		[LastAccessTime] [datetime] NULL,
		[LastAccessMethod] [tinyint] NULL,				--Enum (1 = GET; 2 = POST; 4 = PUT; 8 = DELETE; 16 = HEAD; 32 = SCHEMA)
		[ts] [timestamp] NULL,
		CONSTRAINT [PK_ApiUserFunctionAccess] PRIMARY KEY CLUSTERED (
			[UserFunctionID] ASC
		) ON [PRIMARY]
	) ON [PRIMARY];
END

	USE [' + DB_NAME() + '];
'

	EXEC (@sql)
	FETCH NEXT FROM dbUpdate INTO @dbName
END

CLOSE dbUpdate
DEALLOCATE dbUpdate

DROP TABLE #DatabaseList;