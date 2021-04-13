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

IF NOT EXISTS (SELECT * FROM sys.tables WHERE [object_id] = OBJECT_ID(N''[dbo].[ApiUserToken]''))
BEGIN
	CREATE TABLE [dbo].[ApiUserToken] (
		[UserID] [bigint] NOT NULL,				--BL Required; FK:[ApiUser]
		[RefreshToken] [nvarchar](max) NULL,	--Used for Oauth2 security;
		[IssueTime] [datetime] NULL,
		[ExpireTime] [datetime] NULL,
		CONSTRAINT [PK_ApiUserToken] PRIMARY KEY CLUSTERED (
			[UserID] ASC
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