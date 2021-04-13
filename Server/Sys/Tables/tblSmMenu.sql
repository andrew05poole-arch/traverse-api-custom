SELECT *
INTO #tmpMenu
FROM (
	SELECT CAST(NULL As nvarchar(10)) AppId, CAST(NULL as int) As MenuId, CAST(0 as tinyint) As MenuType, CAST(NULL as int) As ParentId, 
		CAST(NULL as smallint) As [Order], CAST(2 as tinyint) As ObjectType, CAST(0 as bit) As HideYn, 
		CAST(NULL as nvarchar(255)) As PluginName, CAST(NULL as nvarchar(255)) As AssemblyName, 
		CAST(NULL as nvarchar(255)) As FunctionID, CAST(NULL as smallint) As DS
	UNION
	SELECT N'SM', 52400, 0, 100, 900, 8, 0, NULL, NULL, NULL, NULL
	UNION
	SELECT N'SM', 52401, 0, 100, 910, 8, 1, NULL, NULL, NULL, NULL
	UNION
	SELECT N'SM', 5240001, 0, 52400, 10, 2, 0, N'ApiUserAccountPlugin', N'OSI.TraverseApi.Client.dll', N'56EFF7E0-9758-4408-A055-239B537E9F28', 2
	UNION
	SELECT N'SM', 5240002, 0, 52400, 90, 2, 1, N'ApiFunctionPlugin', N'OSI.TraverseApi.Client.dll', NULL, NULL
	UNION
	SELECT N'SM', 5240003, 0, 52400, 20, 2, 0, N'ApiInfoPlugin', N'OSI.TraverseApi.Client.dll', NULL, NULL
	UNION
	SELECT N'SM', 5240101, 0, 52401, 95, 2, 1, N'ApiBuildMaintScriptPlugin', N'OSI.TraverseApi.Client.dll', NULL, NULL
) x;

--Insert new records
INSERT INTO dbo.tblSmMenu (AppId, MenuId, MenuType, ParentId, [Order], ObjectType, HideYn, PluginName, AssemblyName, FunctionID, DS)
SELECT t.AppId, t.MenuId, t.MenuType, t.ParentId, t.[Order], t.ObjectType, t.HideYn, t.PluginName, t.AssemblyName, t.FunctionID, t.DS
FROM #tmpMenu t
	LEFT JOIN dbo.tblSmMenu m On t.MenuId = m.MenuId
WHERE t.MenuId IS NOT NULL AND m.MenuId IS NULL;

--Update existing records; being careful to not remove the Hidden flag that may have been reset by an end-user
UPDATE m SET AppId = t.AppId, MenuType = t.MenuType, ParentId = t.ParentId, [Order] = t.[Order], 
	ObjectType = t.ObjectType, PluginName = t.PluginName, AssemblyName = t.AssemblyName,
	FunctionID = t.FunctionID, DS = t.DS
FROM #tmpMenu t
	INNER JOIN dbo.tblSmMenu m ON t.MenuId = m.MenuID
WHERE t.MenuId IS NOT NULL

DROP TABLE #tmpMenu;