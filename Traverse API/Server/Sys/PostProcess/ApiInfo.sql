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

--Table is created by the business layer and managed there. This process will add any new columns and only update the build number

WHILE @@FETCH_STATUS = 0
BEGIN
	SET @sql = N'
	USE [' + @dbName + N'];

IF EXISTS (SELECT * FROM [ApiInfo])
BEGIN
	UPDATE [ApiInfo] SET [Version] = 21103;
END

	USE [' + DB_NAME() + '];
'

	EXEC (@sql)
	FETCH NEXT FROM dbUpdate INTO @dbName
END

CLOSE dbUpdate
DEALLOCATE dbUpdate

DROP TABLE #DatabaseList;