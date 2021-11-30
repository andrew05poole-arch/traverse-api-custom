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

IF NOT EXISTS (SELECT * FROM sys.tables WHERE [object_id] = OBJECT_ID(N''[dbo].[ApiUserFunctionSchema]''))
BEGIN
	CREATE TABLE [dbo].[ApiUserFunctionSchema] (
		[ID] [bigint] NOT NULL IDENTITY(1, 1),
		[UserFunctionID] [bigint] NULL,
		[FieldType] [tinyint] NULL,				--0 = Entity Field; 1 = Custom Field
		[Notes] [nvarchar](max) NULL,			--Additional information to be displayed in documentation
		[FunctionSchemaID] [bigint] NULL,		--ID from Function Schema field; will be null if field type is custom field
		[CustomFieldName] [nvarchar](100) NULL,	--Custom Field Name; will be null if field type is Entity Field
		[ApiFieldName] [nvarchar](128) NULL,	--Api Display Name
		[Hidden] [bit] NULL,					--Hide this field in results; ignore/error in API request payload
		[DefaultValue] [nvarchar](max) NULL,	--Default value when processing new records
		[DateCreated] [datetime] NULL,
		[DateModified] [datetime] NULL,
		[ModifiedBy] [nvarchar](100) NULL,
		[ts] [timestamp] NULL,
		CONSTRAINT [PK_ApiUserFunctionSchema] PRIMARY KEY CLUSTERED (
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