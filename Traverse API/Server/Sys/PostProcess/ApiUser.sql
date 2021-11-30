DECLARE @dbName as sysname, @sql nvarchar(max)

CREATE TABLE #DatabaseList (DbName sysname)

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

IF NOT EXISTS (SELECT * FROM sys.tables WHERE [object_id] = OBJECT_ID(N''[dbo].[ApiUser]''))
BEGIN
	CREATE TABLE [dbo].[ApiUser] (
		[ID] [bigint] NOT NULL,					--BL generated ID
		[EmailAddress] [nvarchar](256) NULL,	--BL Required; Unique
		[Password] [nvarchar](256) NULL,		--BL encrypted
		[Name] [nvarchar](200) NULL,
		[Role] [tinyint] NULL,					--Enum (1 = Consumer; 2 = Viewer; 4 = Admin)
		[Status] [tinyint] NULL,				--Enum (0 = New; 1 = Active; 2 = Renew; 3 = Disabled)
		[ResetPassword] [bit] NULL,				--BL default 0
		[AdditionalInfo] [nvarchar](max) NULL,
		[ExpirationDate] [datetime] NULL,		--Overall expiration date for the client; will be outside of refresh tokens, etc
		[ClientID] [uniqueidentifier] NULL,		--Used for Oauth2 security
		[ClientSecret] [nvarchar](max) NULL,	--Used for Oauth2 security; encrypted
		[DateCreated] [datetime] NULL,
		[DateModified] [datetime] NULL,
		[ModifiedBy] [nvarchar](100) NULL,
		[ts] [timestamp] NULL,
		CONSTRAINT [PK_ApiUser] PRIMARY KEY CLUSTERED (
			[ID] ASC
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