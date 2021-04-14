SELECT *
INTO #tmpLookup
FROM (
	SELECT CAST(NULL as nvarchar(30)) As Id, CAST(NULL as nvarchar(30)) As ReplaceId, 
		CAST(NULL As nvarchar(100)) As [Description], CAST(NULL As tinyint) As [DBId], 
		CAST(NULL As nvarchar(max)) As KeyId, CAST(NULL As nvarchar(max)) As Defaults,
		CAST(NULL As nvarchar(max)) As Enums,CAST(NULL As nvarchar(max)) As Formats,
		CAST(NULL As bit) As Volatile, CAST(NULL as nvarchar(max)) As DataSource
	UNION
	SELECT N'ApiUser' As Id, NULL As ReplaceId, 
		N'Traverse API User' As [Description], 0 As [DBId], 
		N'ID' As KeyId, N'Email Address, Name, Status' As Defaults,
		N'Status:0;New;1;Active;3;Renew;4;Disabled' As Enums, NULL As Formats,
		0 As Volatile, N'SELECT ID, EmailAddress As [Email Address], [Name], [Status] FROM dbo.ApiUser ORDER BY [Email Address], [Name]' As DataSource
	UNION
	SELECT N'ApiFunction' As Id, NULL As ReplaceId,
		N'Traverse API Function' As [Description], 0 As [DBId],
		N'ID' As KeyId, N'Name, AppId' As Defaults, 
		NULL As Enums, NULL As Formats,
		0 As Volatile, N'SELECT ID, [Name], AppId FROM dbo.ApiFunctionHeader ORDER BY [Name]' As DataSource
) x;

DELETE m
FROM #tmpLookup c
	INNER JOIN dbo.tblSysLookup m ON c.Id = m.Id
WHERE c.Id IS NOT NULL;

INSERT INTO dbo.tblSysLookup (Id, ReplaceId, [Description], [DBId], DataSource, KeyId, Defaults, Enums, Formats, Volatile)
SELECT Id, ReplaceId, [Description], [DBId], DataSource, KeyId, Defaults, Enums, Formats, Volatile
FROM #tmpLookup
WHERE Id IS NOT NULL;

DROP TABLE #tmpLookup;