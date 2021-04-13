SELECT *
INTO #PrintHeader
FROM (
	SELECT CAST(NULL as nvarchar(10)) As FormId, CAST(NULL as nvarchar(50)) As Descr, CAST(NULL as bit) As DocumentDeliveryEnabled, 
		CAST(0 as tinyint) As DeliveryEntityType, CAST(NULL as nchar(2)) As AppId, CAST(NULL as nvarchar(30)) As AttachmentName,
		CAST(0 as bit) As ArchiveEnabled, CAST(NULL as nvarchar(255)) As SchemaFileName, CAST(NULL as uniqueidentifier) as DatasourceDesignId
	UNION ALL
	SELECT N'SM ApiPwd', N'Api Password Reset Notification', 1, 0, N'SM', 'Api Consumer Guide', 0, 'ApiPwdNotice.xsd', '0e3ecd3c-86d4-4c42-99df-b04c1b2e11fb'
	) x

DELETE s
FROM #PrintHeader l
	INNER JOIN dbo.tblSysPrintHeader s On l.FormId = s.FormId
WHERE l.FormId IS NOT NULL

INSERT INTO dbo.tblSysPrintHeader (FormId, Descr, DocumentDeliveryEnabled, DeliveryEntityType, AppId, 
	AttachmentName, ArchiveEnabled, SchemaFileName, DatasourceDesignId)
SELECT i.FormId, i.Descr, i.DocumentDeliveryEnabled, i.DeliveryEntityType, i.AppId, 
	i.AttachmentName, i.ArchiveEnabled, i.SchemaFileName, i.DatasourceDesignId
FROM #PrintHeader i
	LEFT JOIN dbo.tblSysPrintHeader v On i.FormId = v.FormId
WHERE i.FormId IS NOT NULL AND v.FormId IS NULL;

DROP TABLE #PrintHeader;