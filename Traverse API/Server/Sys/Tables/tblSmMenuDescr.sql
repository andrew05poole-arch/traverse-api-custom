SELECT *
INTO #tmpMenuDescr
FROM (
	SELECT CAST(NULL as int) As MenuId, CAST(N'ENG' as nvarchar(3)) As [LangId], CAST(NULL as nvarchar(50)) As Descr
	UNION
	SELECT 52400, N'ENG', N'Api Configuration'
	UNION
	SELECT 52401, N'ENG', N'Api Permissions'
	UNION
	SELECT 5240001, N'ENG', N'User Accounts'
	UNION
	SELECT 5240002, N'ENG', N'Function Setup'
	UNION
	SELECT 5240003, N'ENG', N'Api Security Settings'
	UNION
	SELECT 5240101, N'ENG', N'Generate Server Scripts'
) x;

DELETE m
FROM #tmpMenuDescr d
	INNER JOIN dbo.tblSmMenuDescr m ON d.MenuId = m.MenuId AND d.[LangId] = m.[LangId]
WHERE d.MenuId IS NOT NULL;

INSERT INTO dbo.tblSmMenuDescr (MenuId, [LangId], Descr)
SELECT MenuId, [LangId], Descr
FROM #tmpMenuDescr
WHERE MenuId IS NOT NULL;

DROP TABLE #tmpMenuDescr;