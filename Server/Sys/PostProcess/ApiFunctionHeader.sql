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

DELETE FROM ApiFunctionHeader WHERE ID = N''f739011b-53e6-4687-9a1c-a623bbef4066''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''f739011b-53e6-4687-9a1c-a623bbef4066'' As ID, N''AP Distribution Code'' As [Name], N''AP'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''6449839c-88f2-4d8b-876c-1ff128ae34d5''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''6449839c-88f2-4d8b-876c-1ff128ae34d5'' As ID, N''AP Division Code'' As [Name], N''AP'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''436f98cf-c150-401c-8f83-48ae5fd54a6d''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''436f98cf-c150-401c-8f83-48ae5fd54a6d'' As ID, N''AP Open Invoice View'' As [Name], N''AP'' As AppId, 1 As [Type], NULL As Notes, 1 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''7c4cf57e-2e42-46a0-9192-8857fe095092''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''7c4cf57e-2e42-46a0-9192-8857fe095092'' As ID, N''AP Payment Batch Code'' As [Name], N''AP'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''709c38e7-4eee-4516-9ff7-9eedafecfc8f''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''709c38e7-4eee-4516-9ff7-9eedafecfc8f'' As ID, N''AP Priority Code'' As [Name], N''AP'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''d540fd85-d71b-4d68-a306-79b3d6bd7860''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''d540fd85-d71b-4d68-a306-79b3d6bd7860'' As ID, N''AP Summary Open Invoice'' As [Name], N''AP'' As AppId, 3 As [Type], NULL As Notes, 1 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''9d107a2e-8ab9-4b76-93f4-bd21b70b806a''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''9d107a2e-8ab9-4b76-93f4-bd21b70b806a'' As ID, N''AP Terms Code'' As [Name], N''AP'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''29962cf5-40bd-477a-9f4e-17d0ef20824c''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''29962cf5-40bd-477a-9f4e-17d0ef20824c'' As ID, N''AP Transaction'' As [Name], N''AP'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''76ea6fbb-5ccd-4b7a-9a5e-6a7abec5ff07''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''76ea6fbb-5ccd-4b7a-9a5e-6a7abec5ff07'' As ID, N''AP Transaction Batch Code'' As [Name], N''AP'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''5d148e4a-88b7-464b-9481-f4ecac0be852''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''5d148e4a-88b7-464b-9481-f4ecac0be852'' As ID, N''AP Transaction Detail'' As [Name], N''AP'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''f76289db-00e0-483a-a5d6-6fbabb9abc7b''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''f76289db-00e0-483a-a5d6-6fbabb9abc7b'' As ID, N''AP Transaction Lot'' As [Name], N''AP'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''6fb36f2b-18d3-45a9-9c13-708176cf53cc''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''6fb36f2b-18d3-45a9-9c13-708176cf53cc'' As ID, N''AP Transaction Serial'' As [Name], N''AP'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''1ab8e9d6-9968-4cfd-84eb-407ecb081810''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''1ab8e9d6-9968-4cfd-84eb-407ecb081810'' As ID, N''AP Vendor'' As [Name], N''AP'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''e891b453-42fd-47ca-a5d3-0e14c976f46c''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''e891b453-42fd-47ca-a5d3-0e14c976f46c'' As ID, N''AP Vendor Class Code'' As [Name], N''AP'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''0cd2dfd9-6635-49e6-9865-a3daefcb5e30''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''0cd2dfd9-6635-49e6-9865-a3daefcb5e30'' As ID, N''AR Cash Receipt'' As [Name], N''AR'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''aaf69188-5299-40bd-88ac-36eaeba9e4aa''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''aaf69188-5299-40bd-88ac-36eaeba9e4aa'' As ID, N''AR Cash Receipt Batch Code'' As [Name], N''AR'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''be83f12d-c193-4491-94f6-c680fad279f7''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''be83f12d-c193-4491-94f6-c680fad279f7'' As ID, N''AR Cash Receipt Detail'' As [Name], N''AR'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''5199cf70-a861-42cd-b03a-9dc53742a12e''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''5199cf70-a861-42cd-b03a-9dc53742a12e'' As ID, N''AR Class Code'' As [Name], N''AR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''19432034-bf3e-411a-b410-8362abbe4aa6''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''19432034-bf3e-411a-b410-8362abbe4aa6'' As ID, N''AR Customer'' As [Name], N''AR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''4fe336c1-83d6-4128-8a07-14327f26c240''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''4fe336c1-83d6-4128-8a07-14327f26c240'' As ID, N''AR Customer Ship To'' As [Name], N''AR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''b0883982-0a87-481d-bc75-75543fc8e148''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''b0883982-0a87-481d-bc75-75543fc8e148'' As ID, N''AR Distribution Code'' As [Name], N''AR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''4285e088-483a-4134-8802-4c77d0ceb599''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''4285e088-483a-4134-8802-4c77d0ceb599'' As ID, N''AR Open Invoice View'' As [Name], N''AR'' As AppId, 1 As [Type], NULL As Notes, 1 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''521d9fab-f8ba-4f88-b9f9-c34f7d6b186b''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''521d9fab-f8ba-4f88-b9f9-c34f7d6b186b'' As ID, N''AR Payment Method'' As [Name], N''AR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''f5cf655c-43be-4354-958e-a488ee349d84''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''f5cf655c-43be-4354-958e-a488ee349d84'' As ID, N''AR Sales Account'' As [Name], N''AR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''5fa0617b-c7e1-4d69-b801-b88e2c351283''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''5fa0617b-c7e1-4d69-b801-b88e2c351283'' As ID, N''AR Sales Rep'' As [Name], N''AR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''da2e1101-2613-41c8-bd1c-7e3b67301f70''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''da2e1101-2613-41c8-bd1c-7e3b67301f70'' As ID, N''AR Sales Rep Commission Detail'' As [Name], N''AR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''576b26ff-38b3-478d-89e0-1c0dfa0e9200''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''576b26ff-38b3-478d-89e0-1c0dfa0e9200'' As ID, N''AR Ship Method Code'' As [Name], N''AR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''2a22a47a-0365-4420-9692-97186bda66fd''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''2a22a47a-0365-4420-9692-97186bda66fd'' As ID, N''AR Summary Open Invoice'' As [Name], N''AR'' As AppId, 3 As [Type], NULL As Notes, 1 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''2b8e575a-9294-4cea-b452-a9472d821c46''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''2b8e575a-9294-4cea-b452-a9472d821c46'' As ID, N''AR Terms Code'' As [Name], N''AR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''3f247a50-e0a6-45d1-9a0b-9dde98434ae8''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''3f247a50-e0a6-45d1-9a0b-9dde98434ae8'' As ID, N''AR Transaction Batch Code'' As [Name], N''AR'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''15353e23-a8e4-4938-8850-90146632778b''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''15353e23-a8e4-4938-8850-90146632778b'' As ID, N''AR Transaction Order'' As [Name], N''AR'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''319578c4-e2b8-45a8-b03b-0d0416188764''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''319578c4-e2b8-45a8-b03b-0d0416188764'' As ID, N''AR Transaction Order Detail'' As [Name], N''AR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''deceb1af-3284-47db-93c2-3206a59a79c7''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''deceb1af-3284-47db-93c2-3206a59a79c7'' As ID, N''AR Transaction Order Lot'' As [Name], N''AR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''a482c496-64fd-4331-ae65-b899a139a14f''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''a482c496-64fd-4331-ae65-b899a139a14f'' As ID, N''AR Transaction Order Serial'' As [Name], N''AR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''c6cc1c81-0c04-484b-96aa-358afd3f2352''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''c6cc1c81-0c04-484b-96aa-358afd3f2352'' As ID, N''AR Transaction Payment'' As [Name], N''AR'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''f20ae7a0-3ab6-4586-8c5c-6a57f995778d''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''f20ae7a0-3ab6-4586-8c5c-6a57f995778d'' As ID, N''BM BOM Detail'' As [Name], N''BM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''8e35af8e-7737-48a7-8c68-5e5d15ccdc93''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''8e35af8e-7737-48a7-8c68-5e5d15ccdc93'' As ID, N''BM BOM Header'' As [Name], N''BM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''0cc89168-c4d8-40f9-8973-5907819db1cf''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''0cc89168-c4d8-40f9-8973-5907819db1cf'' As ID, N''BM Labor Account Code'' As [Name], N''BM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''5644d2f4-5b39-4c0a-adfb-52c0254447e6''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''5644d2f4-5b39-4c0a-adfb-52c0254447e6'' As ID, N''BR Adjustment Detail'' As [Name], N''BR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''7fa08bce-0ad5-4c92-a70f-ac77d7253a4c''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''7fa08bce-0ad5-4c92-a70f-ac77d7253a4c'' As ID, N''BR Adjustment Header'' As [Name], N''BR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''b40873ef-c0f0-4666-9f83-ddb6bd2ed406''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''b40873ef-c0f0-4666-9f83-ddb6bd2ed406'' As ID, N''BR Deposit Detail'' As [Name], N''BR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''dfd44a2f-0ebb-4a5b-9d5c-49b12f8e143d''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''dfd44a2f-0ebb-4a5b-9d5c-49b12f8e143d'' As ID, N''BR Deposit Header'' As [Name], N''BR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''6b501595-0464-4903-a032-96b296e0e06e''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''6b501595-0464-4903-a032-96b296e0e06e'' As ID, N''BR Disbursement Detail'' As [Name], N''BR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''78bd0933-a05b-4437-856c-e6ad7fd84213''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''78bd0933-a05b-4437-856c-e6ad7fd84213'' As ID, N''BR Disbursement Header'' As [Name], N''BR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''60135511-7903-49fc-9990-3d3b74b86c9e''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''60135511-7903-49fc-9990-3d3b74b86c9e'' As ID, N''BR Transfer'' As [Name], N''BR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''08db86c1-3b10-4ea9-a4a4-c3fc80d32d4e''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''08db86c1-3b10-4ea9-a4a4-c3fc80d32d4e'' As ID, N''BR Transfer Detail'' As [Name], N''BR'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''0cb0dcfd-21df-4d7b-8d85-396d73fbe4ab''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''0cb0dcfd-21df-4d7b-8d85-396d73fbe4ab'' As ID, N''CM Activity'' As [Name], N''CM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''2953b4d1-2207-4605-b731-164346e64289''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''2953b4d1-2207-4605-b731-164346e64289'' As ID, N''CM Activity Type'' As [Name], N''CM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''3f46f03f-d22a-4adb-8624-261ba5e06273''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''3f46f03f-d22a-4adb-8624-261ba5e06273'' As ID, N''CM Campaign'' As [Name], N''CM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''6b645f37-7e66-4a6e-9f5c-5f0aed57304f''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''6b645f37-7e66-4a6e-9f5c-5f0aed57304f'' As ID, N''CM Campaign Detail'' As [Name], N''CM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''47da1657-681f-40bf-b42c-20baa2e5860c''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''47da1657-681f-40bf-b42c-20baa2e5860c'' As ID, N''CM Campaign Type'' As [Name], N''CM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''12db014a-92f5-4c3b-9f9d-a8b897c43f38''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''12db014a-92f5-4c3b-9f9d-a8b897c43f38'' As ID, N''CM Contact'' As [Name], N''CM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''7253204b-8269-4bb2-a4b1-af24e2b1d852''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''7253204b-8269-4bb2-a4b1-af24e2b1d852'' As ID, N''CM Contact Address'' As [Name], N''CM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''abccb357-8841-4679-a3ec-8a531257123d''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''abccb357-8841-4679-a3ec-8a531257123d'' As ID, N''CM Contact Method'' As [Name], N''CM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''a19ec440-2984-4386-aec2-d2ef9bd81107''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''a19ec440-2984-4386-aec2-d2ef9bd81107'' As ID, N''CM Contact Method Type'' As [Name], N''CM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''0a7833a5-3ed3-4cb7-91f3-ad5e9b511071''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''0a7833a5-3ed3-4cb7-91f3-ad5e9b511071'' As ID, N''CM Contact Status Code'' As [Name], N''CM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''45616b3a-53ef-4bc4-9547-12e235348a58''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''45616b3a-53ef-4bc4-9547-12e235348a58'' As ID, N''CM Opportunity'' As [Name], N''CM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''f7c03896-2647-4d29-8087-049484488d77''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''f7c03896-2647-4d29-8087-049484488d77'' As ID, N''CM Opportunity Probability Code'' As [Name], N''CM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''87a58c61-81e6-4d10-aa1b-436ddf151ee8''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''87a58c61-81e6-4d10-aa1b-436ddf151ee8'' As ID, N''CM Opportunity Resolution Code'' As [Name], N''CM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''f57bcdac-836b-49cd-9c27-15953f4fe106''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''f57bcdac-836b-49cd-9c27-15953f4fe106'' As ID, N''CM Opportunity Status Code'' As [Name], N''CM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''02c10285-3ae9-4b8d-b2ce-a0ac846c623b''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''02c10285-3ae9-4b8d-b2ce-a0ac846c623b'' As ID, N''CM Task'' As [Name], N''CM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''cde8c778-7dc2-4704-bb3a-a5bb9942bff3''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''cde8c778-7dc2-4704-bb3a-a5bb9942bff3'' As ID, N''CM Task Type'' As [Name], N''CM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''89a94c4e-8378-445b-827e-5f3c747f5417''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''89a94c4e-8378-445b-827e-5f3c747f5417'' As ID, N''GL Account Class'' As [Name], N''GL'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''c5523178-aba9-4b6d-b587-79adb07195a8''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''c5523178-aba9-4b6d-b587-79adb07195a8'' As ID, N''GL Account Type'' As [Name], N''GL'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''18351085-85fb-4f8e-ac92-a245a6532c1d''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''18351085-85fb-4f8e-ac92-a245a6532c1d'' As ID, N''GL Chart of Account'' As [Name], N''GL'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''2ebaa4e5-c247-4081-96ba-6e701a26023f''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''2ebaa4e5-c247-4081-96ba-6e701a26023f'' As ID, N''GL Transaction'' As [Name], N''GL'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''4461bc39-de8b-4ed0-844b-763f22d0cf5d''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''4461bc39-de8b-4ed0-844b-763f22d0cf5d'' As ID, N''GL Transaction Batch Code'' As [Name], N''GL'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''54894301-b3d8-4e1d-9d50-9117cbe2e447''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''54894301-b3d8-4e1d-9d50-9117cbe2e447'' As ID, N''GL Transaction Write'' As [Name], N''GL'' As AppId, 2 As [Type], NULL As Notes, 4 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''d3ea4867-c8bd-466b-85b8-2ec21ca1809d''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''d3ea4867-c8bd-466b-85b8-2ec21ca1809d'' As ID, N''IN Account Code'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''d0a66974-8f63-4b77-9acc-ba613abb4c81''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''d0a66974-8f63-4b77-9acc-ba613abb4c81'' As ID, N''IN Hazmat Code'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''4c906b6c-b5c9-48ce-86ba-85edde8d5950''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''4c906b6c-b5c9-48ce-86ba-85edde8d5950'' As ID, N''IN Item'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''eb96f902-1aa7-4311-84fa-e7ce37866f07''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''eb96f902-1aa7-4311-84fa-e7ce37866f07'' As ID, N''IN Item Location'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''20d03be8-45b3-4cfd-9c92-aed2163ae355''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''20d03be8-45b3-4cfd-9c92-aed2163ae355'' As ID, N''IN Item Location Vendor'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''70d1f05b-1e0b-4101-a535-2b2cec2834eb''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''70d1f05b-1e0b-4101-a535-2b2cec2834eb'' As ID, N''IN Item Picture'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''5472f400-c9c8-475d-980d-fd17fc7adc21''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''5472f400-c9c8-475d-980d-fd17fc7adc21'' As ID, N''IN Item Price'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''51ad67f5-7693-43b0-a4ff-9040114ed2b0''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''51ad67f5-7693-43b0-a4ff-9040114ed2b0'' As ID, N''IN Item Qty'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''58296d2d-95f3-4681-ad2a-a7aeed57f064''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''58296d2d-95f3-4681-ad2a-a7aeed57f064'' As ID, N''IN Item Unit of Measure'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''0cfc125a-377a-45ef-a0df-b749e1d2dc00''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''0cfc125a-377a-45ef-a0df-b749e1d2dc00'' As ID, N''IN Item Vendor'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''2cb5e090-5d0a-4f0d-b107-3414af42e341''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''2cb5e090-5d0a-4f0d-b107-3414af42e341'' As ID, N''IN Item Vendor Detail'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''ba49f772-f43d-4039-a040-535e667121c6''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''ba49f772-f43d-4039-a040-535e667121c6'' As ID, N''IN Location'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''f3d24fac-b3e6-497b-984a-5b64922455f0''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''f3d24fac-b3e6-497b-984a-5b64922455f0'' As ID, N''IN Location Transfer'' As [Name], N''IN'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''186a8d00-0303-4f99-b842-866c998ce8ee''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''186a8d00-0303-4f99-b842-866c998ce8ee'' As ID, N''IN Location Transfer Batch Code'' As [Name], N''IN'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''f4416e23-775b-4928-9518-0f1000fda003''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''f4416e23-775b-4928-9518-0f1000fda003'' As ID, N''IN Location Transfer Lot'' As [Name], N''IN'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''742f69c8-4a18-469f-be04-c912514037ad''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''742f69c8-4a18-469f-be04-c912514037ad'' As ID, N''IN Location Transfer Serial'' As [Name], N''IN'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''354ebccb-aaba-4977-8c3d-5f86628fab2f''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''354ebccb-aaba-4977-8c3d-5f86628fab2f'' As ID, N''IN Lot Number'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''30f2a7fa-a5a0-4cca-866d-1d9408741324''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''30f2a7fa-a5a0-4cca-866d-1d9408741324'' As ID, N''IN Product Line'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''66cac34f-4f96-4bd8-8442-3b7cc9ca5ab4''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''66cac34f-4f96-4bd8-8442-3b7cc9ca5ab4'' As ID, N''IN Sales Category'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''84ce93b9-1438-405d-8939-cc5626c6d76c''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''84ce93b9-1438-405d-8939-cc5626c6d76c'' As ID, N''IN Serial Number'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 1 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''95ad3b62-5edb-461c-bfb8-8d930768f0eb''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''95ad3b62-5edb-461c-bfb8-8d930768f0eb'' As ID, N''IN Ship-To Address'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''53955c2f-a1a0-4f9b-9d27-2ed2161f8cd3''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''53955c2f-a1a0-4f9b-9d27-2ed2161f8cd3'' As ID, N''IN Transaction Adjustment'' As [Name], N''IN'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''5dbecf27-72cd-44ca-a125-d622849910bc''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''5dbecf27-72cd-44ca-a125-d622849910bc'' As ID, N''IN Transaction Adjustment Lot'' As [Name], N''IN'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''45e50ffe-fe7e-44af-a266-d4396df42828''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''45e50ffe-fe7e-44af-a266-d4396df42828'' As ID, N''IN Transaction Adjustment Serial'' As [Name], N''IN'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''d1627a5d-fe15-498a-9e1c-9d89469dc223''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''d1627a5d-fe15-498a-9e1c-9d89469dc223'' As ID, N''IN Transaction Batch Code'' As [Name], N''IN'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''ec7c5f49-eeef-4b4c-909d-24e8e723ca7d''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''ec7c5f49-eeef-4b4c-909d-24e8e723ca7d'' As ID, N''IN Transaction Purchase'' As [Name], N''IN'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''5ba5c0a5-f881-4270-9846-04e4f9030351''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''5ba5c0a5-f881-4270-9846-04e4f9030351'' As ID, N''IN Transaction Purchase Lot'' As [Name], N''IN'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''9421b02e-f63d-4b30-b3ba-d39c21fec275''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''9421b02e-f63d-4b30-b3ba-d39c21fec275'' As ID, N''IN Transaction Purchase Serial'' As [Name], N''IN'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''ccc87e99-9f77-4fde-bafe-e236c7d8aa64''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''ccc87e99-9f77-4fde-bafe-e236c7d8aa64'' As ID, N''IN Transaction Sale'' As [Name], N''IN'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''12cd7423-946c-4192-8315-632fb1d761e3''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''12cd7423-946c-4192-8315-632fb1d761e3'' As ID, N''IN Transaction Sale Lot'' As [Name], N''IN'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''a20afb9d-6bc4-4a1e-aa9d-e5ffb97614db''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''a20afb9d-6bc4-4a1e-aa9d-e5ffb97614db'' As ID, N''IN Transaction Sale Serial'' As [Name], N''IN'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''7c3a7e4b-a59a-4ddb-ad49-cfc6e7d3e923''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''7c3a7e4b-a59a-4ddb-ad49-cfc6e7d3e923'' As ID, N''IN Unit of Measure Entry'' As [Name], N''IN'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''b6295f0f-6b7e-497a-80bc-7317ba50d4af''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''b6295f0f-6b7e-497a-80bc-7317ba50d4af'' As ID, N''MB Bill of Material Assembly'' As [Name], N''MB'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''444cb952-4541-4ad5-8abb-8f60e2edf03b''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''444cb952-4541-4ad5-8abb-8f60e2edf03b'' As ID, N''MB Bill of Material Assembly Detail'' As [Name], N''MB'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''08a3502b-489e-4409-a436-8c2de72bf311''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''08a3502b-489e-4409-a436-8c2de72bf311'' As ID, N''MB Bill of Material Assembly Routing'' As [Name], N''MB'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''f74020a2-db22-4c99-94e6-dede04e49cc6''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''f74020a2-db22-4c99-94e6-dede04e49cc6'' As ID, N''MB Cost Group'' As [Name], N''MB'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''711eaa61-e71c-45d1-b4ff-a570ad587375''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''711eaa61-e71c-45d1-b4ff-a570ad587375'' As ID, N''MB Media Group'' As [Name], N''MB'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''242e7391-0a22-480d-98dd-d63e6ccafa11''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''242e7391-0a22-480d-98dd-d63e6ccafa11'' As ID, N''MB Media Group Detail'' As [Name], N''MB'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''840f79bf-3780-4dc4-ae08-992e72f80f1a''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''840f79bf-3780-4dc4-ae08-992e72f80f1a'' As ID, N''MP Edit Released Order Material Summary'' As [Name], N''MP'' As AppId, 3 As [Type], N''use parent_id to pull in the parent ReqId for posting a new record'' As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''7a08fe55-8de2-4280-9b08-a447e5ebe3d8''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''7a08fe55-8de2-4280-9b08-a447e5ebe3d8'' As ID, N''MP Edit Released Order Subcontract Summary'' As [Name], N''MP'' As AppId, 3 As [Type], N''use parent_id to pull in the parent ReqId for posting a new record'' As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''8ae5956c-2346-481d-bab0-af960c4f1ed1''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''8ae5956c-2346-481d-bab0-af960c4f1ed1'' As ID, N''MP Edit Released Order Time Summary'' As [Name], N''MP'' As AppId, 3 As [Type], N''use parent_id to pull in the parent ReqId for posting a new record'' As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''8f1c5a0f-5a92-4970-831a-09f2c1f242f7''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''8f1c5a0f-5a92-4970-831a-09f2c1f242f7'' As ID, N''MP Generate Requirement'' As [Name], N''MP'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''421b44b2-cbed-4cee-a368-adc3f9f3f4fc''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''421b44b2-cbed-4cee-a368-adc3f9f3f4fc'' As ID, N''MP Order'' As [Name], N''MP'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''5ce0a03f-a994-48da-a3a3-53d152e24c08''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''5ce0a03f-a994-48da-a3a3-53d152e24c08'' As ID, N''MP Production Order Release'' As [Name], N''MP'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''2ea6d076-b0ad-468e-82c1-1dd683a0526a''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''2ea6d076-b0ad-468e-82c1-1dd683a0526a'' As ID, N''MP Record Production Activity Material'' As [Name], N''MP'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''6e759195-5a89-421f-a653-3984a8c4e697''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''6e759195-5a89-421f-a653-3984a8c4e697'' As ID, N''MP Record Production Activity Material Ext'' As [Name], N''MP'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''adcd607c-7bfd-4d13-a162-bf2b1c95bf64''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''adcd607c-7bfd-4d13-a162-bf2b1c95bf64'' As ID, N''MP Record Production Activity Material Serial'' As [Name], N''MP'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''bb00d8f9-8f7b-4e45-92ec-3dfd6fd48b29''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''bb00d8f9-8f7b-4e45-92ec-3dfd6fd48b29'' As ID, N''MP Record Production Activity Material Summary Ext'' As [Name], N''MP'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''d205c62b-951e-4a8c-976b-e1a2f6ed6449''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''d205c62b-951e-4a8c-976b-e1a2f6ed6449'' As ID, N''MP Record Production Activity Subcontract'' As [Name], N''MP'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''94b3c7e8-0c49-4f2a-a3bd-a5fd98791447''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''94b3c7e8-0c49-4f2a-a3bd-a5fd98791447'' As ID, N''MP Record Production Activity Time'' As [Name], N''MP'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''27ce2d14-8255-4a69-b784-f01a5446735c''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''27ce2d14-8255-4a69-b784-f01a5446735c'' As ID, N''MR Labor Type'' As [Name], N''MR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''224ced3f-aee6-4442-a388-43633fc86c79''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''224ced3f-aee6-4442-a388-43633fc86c79'' As ID, N''MR Labor Type Employee'' As [Name], N''MR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''946226f5-a399-4a56-a9fc-ff5be6c4803e''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''946226f5-a399-4a56-a9fc-ff5be6c4803e'' As ID, N''MR Machine Group'' As [Name], N''MR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''7cc501bf-1359-42aa-be46-36a2e6f5d6e5''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''7cc501bf-1359-42aa-be46-36a2e6f5d6e5'' As ID, N''MR Machine Group Labor Type'' As [Name], N''MR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''38d6128b-6fc7-4af7-879c-a850fd7a6a80''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''38d6128b-6fc7-4af7-879c-a850fd7a6a80'' As ID, N''MR Operation'' As [Name], N''MR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''2138815d-b74a-4ff9-b0db-c27dcd80a64b''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''2138815d-b74a-4ff9-b0db-c27dcd80a64b'' As ID, N''MR Operation Subcontracted'' As [Name], N''MR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''386a4813-2030-48af-83b8-8ee5c1de9674''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''386a4813-2030-48af-83b8-8ee5c1de9674'' As ID, N''MR Operation Tooling'' As [Name], N''MR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''68355646-1f88-440b-9362-a9d07271b3f7''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''68355646-1f88-440b-9362-a9d07271b3f7'' As ID, N''MR Routing'' As [Name], N''MR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''306b8ce5-45ea-479a-8cd1-7845efdbf008''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''306b8ce5-45ea-479a-8cd1-7845efdbf008'' As ID, N''MR Routing Detail'' As [Name], N''MR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''ed39f267-470d-490a-bb4c-92b920e7789c''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''ed39f267-470d-490a-bb4c-92b920e7789c'' As ID, N''MR Tooling'' As [Name], N''MR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''ebae2996-26cf-4897-9bad-e06044b5a208''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''ebae2996-26cf-4897-9bad-e06044b5a208'' As ID, N''MR Work Center'' As [Name], N''MR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''b3734f51-da1e-48c2-808d-ef8bf8ac0a1f''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''b3734f51-da1e-48c2-808d-ef8bf8ac0a1f'' As ID, N''MR Work Center Machine Group'' As [Name], N''MR'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''6d6c7896-7ccd-46b7-92cd-d28d13a9cfff''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''6d6c7896-7ccd-46b7-92cd-d28d13a9cfff'' As ID, N''PA Department'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''d30250e2-be85-41e7-ac07-f8c1014a485f''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''d30250e2-be85-41e7-ac07-f8c1014a485f'' As ID, N''PA Department Allocation'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''dbee82a4-b9f7-4576-808c-67babd681bb1''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''dbee82a4-b9f7-4576-808c-67babd681bb1'' As ID, N''PA Earning Code'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''da960bc6-656a-4d13-9448-83aba9777d6b''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''da960bc6-656a-4d13-9448-83aba9777d6b'' As ID, N''PA Earning Type'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''0391bf9f-e1dc-488f-9805-ef16c3b94a5a''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''0391bf9f-e1dc-488f-9805-ef16c3b94a5a'' As ID, N''PA Employee'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''1beb4ac3-d76d-4633-8ee7-5570f06279fa''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''1beb4ac3-d76d-4633-8ee7-5570f06279fa'' As ID, N''PA Employee Bonus'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''0e708eb0-4e71-4afb-a6c0-e3127f3ced60''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''0e708eb0-4e71-4afb-a6c0-e3127f3ced60'' As ID, N''PA Employee Deduction'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''728ce45b-bafd-4507-89fc-f47801e1887b''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''728ce45b-bafd-4507-89fc-f47801e1887b'' As ID, N''PA Employee Deduction Code'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''38f9c3bf-f0ef-41a7-8d9a-1cb612c9e648''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''38f9c3bf-f0ef-41a7-8d9a-1cb612c9e648'' As ID, N''PA Employee Deduction Employer'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''e7d4ac60-e05c-4a4e-95aa-c950d50990a9''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''e7d4ac60-e05c-4a4e-95aa-c950d50990a9'' As ID, N''PA Employee Detail'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''4b7f1656-ae4e-40a1-88ff-8174bf592544''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''4b7f1656-ae4e-40a1-88ff-8174bf592544'' As ID, N''PA Employee Direct Deposit'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''7b347195-0f5d-4c31-ae93-870003cadd1f''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''7b347195-0f5d-4c31-ae93-870003cadd1f'' As ID, N''PA Employee Earning Code'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''927a3c24-188a-4d07-8cbd-211e44254301''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''927a3c24-188a-4d07-8cbd-211e44254301'' As ID, N''PA Employee Education'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''e5ea00e2-e5be-4214-8f1d-6bbd4abce64f''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''e5ea00e2-e5be-4214-8f1d-6bbd4abce64f'' As ID, N''PA Employee Federal Withholding'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''5f388695-333c-4184-bd27-a56110dbecd3''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''5f388695-333c-4184-bd27-a56110dbecd3'' As ID, N''PA Employee Leave Code'' As [Name], N''PA'' As AppId, 1 As [Type], N''Employee Leave doesn''''t support the Update process.'' As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''76ab9209-d7f6-4ddf-b168-8f35ffc2c833''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''76ab9209-d7f6-4ddf-b168-8f35ffc2c833'' As ID, N''PA Employee Local Withholding'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''44b01e7a-1712-484d-af07-af6626a000d0''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''44b01e7a-1712-484d-af07-af6626a000d0'' As ID, N''PA Employee Rate Change'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''08a4911e-ca66-4009-982c-7538524db173''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''08a4911e-ca66-4009-982c-7538524db173'' As ID, N''PA Employee State Withholding'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''17e12fe8-2fcf-4461-9023-3c937cf9e9e6''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''17e12fe8-2fcf-4461-9023-3c937cf9e9e6'' As ID, N''PA Employer Cost Code'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''84e805e7-f613-43a9-8406-2afff8c2444a''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''84e805e7-f613-43a9-8406-2afff8c2444a'' As ID, N''PA Federal Status Code'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''66da7c82-7a63-4b7b-b375-cebc04f2ff42''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''66da7c82-7a63-4b7b-b375-cebc04f2ff42'' As ID, N''PA Formula'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''cf2a5d3a-a65b-4aba-a879-bb53f739b503''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''cf2a5d3a-a65b-4aba-a879-bb53f739b503'' As ID, N''PA Labor Class'' As [Name], N''PA'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''c03fe183-85da-4432-bd5d-8a33a6e7e6ad''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''c03fe183-85da-4432-bd5d-8a33a6e7e6ad'' As ID, N''PA Leave Code Detail'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''3eab000d-86d5-4bcc-bff7-1e50623256b3''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''3eab000d-86d5-4bcc-bff7-1e50623256b3'' As ID, N''PA Leave Code Header'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''555b872e-0b39-468f-9c48-e21cb118e37f''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''555b872e-0b39-468f-9c48-e21cb118e37f'' As ID, N''PA Local Status Code'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''a91172b7-2e34-4cbf-9eef-fd4b686b1cd8''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''a91172b7-2e34-4cbf-9eef-fd4b686b1cd8'' As ID, N''PA State Status Code'' As [Name], N''PA'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''0924e7dd-19af-4466-a078-6a78a25024c2''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''0924e7dd-19af-4466-a078-6a78a25024c2'' As ID, N''PA Transaction Earn'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''3dc96758-a62b-4c56-9585-3c3d73bc0850''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''3dc96758-a62b-4c56-9585-3c3d73bc0850'' As ID, N''PA Transaction Employee Deduction'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''eb6097ff-f323-42ca-9d3a-7401d55d6a58''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''eb6097ff-f323-42ca-9d3a-7401d55d6a58'' As ID, N''PA Transaction Employer Cost'' As [Name], N''PA'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''6dea7340-33c5-4ff7-931b-91c8fddcc715''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''6dea7340-33c5-4ff7-931b-91c8fddcc715'' As ID, N''PA Transactions'' As [Name], N''PA'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''4c28fe0e-952b-4b36-88f1-a73c73e5c2aa''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''4c28fe0e-952b-4b36-88f1-a73c73e5c2aa'' As ID, N''PC Distribution Code'' As [Name], N''JC'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''f1b3351e-5782-4437-9409-3b5434819990''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''f1b3351e-5782-4437-9409-3b5434819990'' As ID, N''PC Employee Rate'' As [Name], N''JC'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''8540c0dd-f269-4eb5-8613-3d2a04f2c646''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''8540c0dd-f269-4eb5-8613-3d2a04f2c646'' As ID, N''PC Overhead Allocation Code'' As [Name], N''JC'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''4f29737d-10e3-4c39-bb6f-06d59c4ef62b''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''4f29737d-10e3-4c39-bb6f-06d59c4ef62b'' As ID, N''PC Phase Code'' As [Name], N''JC'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''eab33a8d-24e4-4334-9741-1a97c917fc80''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''eab33a8d-24e4-4334-9741-1a97c917fc80'' As ID, N''PC Project'' As [Name], N''JC'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''8e2c0ea9-a370-4802-bb6b-b98d1a8c66fa''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''8e2c0ea9-a370-4802-bb6b-b98d1a8c66fa'' As ID, N''PC Project Extension'' As [Name], N''JC'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''4db715d9-0b0a-416b-a599-a89d06bf342f''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''4db715d9-0b0a-416b-a599-a89d06bf342f'' As ID, N''PC Project Site Info'' As [Name], N''JC'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''326c00b9-2ce2-4347-937c-27722a7a0082''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''326c00b9-2ce2-4347-937c-27722a7a0082'' As ID, N''PC Project Task'' As [Name], N''JC'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''a052411f-915d-4cf4-b3df-1d778abc5f4e''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''a052411f-915d-4cf4-b3df-1d778abc5f4e'' As ID, N''PC Rate Code'' As [Name], N''JC'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''3482024a-a405-487b-8b96-6fa72e5fcc05''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''3482024a-a405-487b-8b96-6fa72e5fcc05'' As ID, N''PC Task Code'' As [Name], N''JC'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''7358e34e-fb2f-428c-b110-f595cb4eb69d''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''7358e34e-fb2f-428c-b110-f595cb4eb69d'' As ID, N''PC Time Ticket'' As [Name], N''JC'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''686d3a6e-c2d0-47e0-b143-57dd66890406''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''686d3a6e-c2d0-47e0-b143-57dd66890406'' As ID, N''PC Transaction'' As [Name], N''JC'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''121c2912-669e-4126-84cc-329aa27d17ac''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''121c2912-669e-4126-84cc-329aa27d17ac'' As ID, N''PC Transaction Batch Code'' As [Name], N''JC'' As AppId, 2 As [Type], N'' Journal status and time ticket journal status fields cannot be updated  by means of the POST method these can only be changed by means of the PUT method.'' As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''fd74c61a-75ac-4f52-a78c-441c5e4e32ea''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''fd74c61a-75ac-4f52-a78c-441c5e4e32ea'' As ID, N''PC Transaction Lot'' As [Name], N''JC'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''6d382353-e38e-47ab-a10d-6bcfecc49bbc''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''6d382353-e38e-47ab-a10d-6bcfecc49bbc'' As ID, N''PC Transaction Serial'' As [Name], N''JC'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''f9b9ee5d-b001-468c-9c84-b13224fa4ae9''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''f9b9ee5d-b001-468c-9c84-b13224fa4ae9'' As ID, N''PO Requisition'' As [Name], N''PO'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''9e350f53-2598-4a88-8531-7fdb58c5cb4b''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''9e350f53-2598-4a88-8531-7fdb58c5cb4b'' As ID, N''PO Ship To Address'' As [Name], N''PO'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''febd955f-42a3-4f95-8290-78af5f534043''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''febd955f-42a3-4f95-8290-78af5f534043'' As ID, N''PO Transaction'' As [Name], N''PO'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''54161fbd-78e9-49de-9ce2-95a7b61dd03d''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''54161fbd-78e9-49de-9ce2-95a7b61dd03d'' As ID, N''PO Transaction Batch Code'' As [Name], N''PO'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''709529b9-0e66-4822-a647-7cfabf388a31''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''709529b9-0e66-4822-a647-7cfabf388a31'' As ID, N''PO Transaction Detail'' As [Name], N''PO'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''2115b53a-fa2d-4993-bfe1-c8c443f4b790''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''2115b53a-fa2d-4993-bfe1-c8c443f4b790'' As ID, N''PO Transaction Invoice'' As [Name], N''PO'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''e34edcee-c7b4-40e7-bbe3-f74fbb36ee54''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''e34edcee-c7b4-40e7-bbe3-f74fbb36ee54'' As ID, N''PO Transaction Invoice Detail'' As [Name], N''PO'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''7255519d-c224-4d3a-bc82-c8f8e5c6a3ec''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''7255519d-c224-4d3a-bc82-c8f8e5c6a3ec'' As ID, N''PO Transaction Invoice Receipt'' As [Name], N''PO'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''a9853003-6824-46d6-ba23-5727e0078c44''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''a9853003-6824-46d6-ba23-5727e0078c44'' As ID, N''PO Transaction Invoice Serial'' As [Name], N''PO'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''32a21dc9-ad28-4ce1-b83c-4c6b486a4395''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''32a21dc9-ad28-4ce1-b83c-4c6b486a4395'' As ID, N''PO Transaction Lot Receipt'' As [Name], N''PO'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''9c002741-779c-40b3-8717-eb310a04187a''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''9c002741-779c-40b3-8717-eb310a04187a'' As ID, N''PO Transaction Receipt'' As [Name], N''PO'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''fe2dbdf4-628a-4260-9c84-51b5be4256cb''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''fe2dbdf4-628a-4260-9c84-51b5be4256cb'' As ID, N''PO Transaction Serial Receipt'' As [Name], N''PO'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''c73815e4-f417-431d-8cf7-b4227faaf25f''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''c73815e4-f417-431d-8cf7-b4227faaf25f'' As ID, N''SD Billing Type'' As [Name], N''SD'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''70dd48bd-6ee6-4c45-9bb0-da93fbf24168''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''70dd48bd-6ee6-4c45-9bb0-da93fbf24168'' As ID, N''SD Equipment Category'' As [Name], N''SD'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''a29fe3e5-99b6-4688-a2b9-afd98539aa14''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''a29fe3e5-99b6-4688-a2b9-afd98539aa14'' As ID, N''SD Labor Code'' As [Name], N''SD'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''82f94046-e961-4f31-9bd6-58cfdf97e1c0''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''82f94046-e961-4f31-9bd6-58cfdf97e1c0'' As ID, N''SM Bank Account'' As [Name], N''SM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''9a955b63-165e-4efb-a9c5-95e99bff7965''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''9a955b63-165e-4efb-a9c5-95e99bff7965'' As ID, N''SM Country Code'' As [Name], N''SM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''d5e4c2b9-3daf-4f40-abce-dc5f6eb8b238''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''d5e4c2b9-3daf-4f40-abce-dc5f6eb8b238'' As ID, N''SM Currency'' As [Name], N''SM'' As AppId, 1 As [Type], NULL As Notes, 1 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''7283a2b0-5690-474b-a7b2-2efd65dce242''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''7283a2b0-5690-474b-a7b2-2efd65dce242'' As ID, N''SM Description Item'' As [Name], N''SM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''4ddf091e-10e2-4ac2-82be-4f31ac50ff9a''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''4ddf091e-10e2-4ac2-82be-4f31ac50ff9a'' As ID, N''SM Employee'' As [Name], N''SM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''c691df07-8b04-4677-9ec1-65d8d9733f15''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''c691df07-8b04-4677-9ec1-65d8d9733f15'' As ID, N''SM Form Number'' As [Name], N''SM'' As AppId, 3 As [Type], NULL As Notes, 7 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''bbe0498a-69d1-45a1-9d2f-2bb92f192660''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''bbe0498a-69d1-45a1-9d2f-2bb92f192660'' As ID, N''SM Location Tax Groups'' As [Name], N''SM'' As AppId, 3 As [Type], NULL As Notes, 1 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''9069fe16-8035-416f-babf-7c68d9f53cb7''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''9069fe16-8035-416f-babf-7c68d9f53cb7'' As ID, N''SM Manage Attachment'' As [Name], N''SM'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''2764c2ba-55d6-46e1-870f-756544da1fdf''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''2764c2ba-55d6-46e1-870f-756544da1fdf'' As ID, N''SM Tax Classes'' As [Name], N''SM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''6f635363-f51a-47f8-aba2-6fdf5c836b85''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''6f635363-f51a-47f8-aba2-6fdf5c836b85'' As ID, N''SM Transaction Number'' As [Name], N''SM'' As AppId, 3 As [Type], NULL As Notes, 7 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''cf9b2505-7e09-4c3f-8fdc-6d0b23eb2347''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''cf9b2505-7e09-4c3f-8fdc-6d0b23eb2347'' As ID, N''SO Contract Pricing Customer'' As [Name], N''SO'' As AppId, 3 As [Type], NULL As Notes, 1 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''2ce4da51-92d4-48a9-91d5-222292d607bd''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''2ce4da51-92d4-48a9-91d5-222292d607bd'' As ID, N''SO Contract Pricing Detail'' As [Name], N''SO'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''649a91db-dcd5-4095-9011-5e6f08b54e66''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''649a91db-dcd5-4095-9011-5e6f08b54e66'' As ID, N''SO Contract Pricing Header'' As [Name], N''SO'' As AppId, 3 As [Type], NULL As Notes, 1 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''45d53952-7c71-4278-bede-f68a71128d23''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''45d53952-7c71-4278-bede-f68a71128d23'' As ID, N''SO Contract Pricing Item'' As [Name], N''SO'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''d4349775-2929-4bf5-adaf-45e0bc3e6ebe''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''d4349775-2929-4bf5-adaf-45e0bc3e6ebe'' As ID, N''SO Customer Level'' As [Name], N''SO'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''fc39876d-606a-4e61-a9a1-9546e36fd527''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''fc39876d-606a-4e61-a9a1-9546e36fd527'' As ID, N''SO Customer Pricing Detail'' As [Name], N''SO'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''87bee63e-09e3-4c59-9738-ad11e0dc2a18''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''87bee63e-09e3-4c59-9738-ad11e0dc2a18'' As ID, N''SO Customer Pricing Header'' As [Name], N''SO'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''bc40c238-13d4-446b-b61d-21a68cb9f952''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''bc40c238-13d4-446b-b61d-21a68cb9f952'' As ID, N''SO Price Calculator'' As [Name], N''SO'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''f9355878-e277-486b-ac94-eb7f587b249a''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''f9355878-e277-486b-ac94-eb7f587b249a'' As ID, N''SO Price Structure'' As [Name], N''SO'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''c906e03d-1d1d-4f95-b466-f214f9c4a7d9''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''c906e03d-1d1d-4f95-b466-f214f9c4a7d9'' As ID, N''SO Price Structure Detail'' As [Name], N''SO'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''8a96e640-d2b7-4197-949d-ba1407532f7f''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''8a96e640-d2b7-4197-949d-ba1407532f7f'' As ID, N''SO Price Structure Detail'' As [Name], N''SO'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''709c38e7-4eee-4516-9ff7-9eedafecfc8e''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''709c38e7-4eee-4516-9ff7-9eedafecfc8e'' As ID, N''SO Price Structure Header'' As [Name], N''SO'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''c41b4f08-bb5b-4361-8243-9b2c71a19f9f''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''c41b4f08-bb5b-4361-8243-9b2c71a19f9f'' As ID, N''SO Reason Code'' As [Name], N''SO'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''a91f0a14-cd46-4b3b-b1b1-bb4b0b64b39d''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''a91f0a14-cd46-4b3b-b1b1-bb4b0b64b39d'' As ID, N''SO Transaction Batch Codes'' As [Name], N''SO'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''765ae38b-af2c-4529-b445-a914ca369a4b''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''765ae38b-af2c-4529-b445-a914ca369a4b'' As ID, N''SO Transaction Order'' As [Name], N''SO'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''c16315f8-8f21-419d-90c1-3176c35dddc6''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As ID, N''SO Transaction Order Detail'' As [Name], N''SO'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''e59b69e9-fce8-47ad-bfec-c4bf934b6e63''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''e59b69e9-fce8-47ad-bfec-c4bf934b6e63'' As ID, N''SO Transaction Order Lot'' As [Name], N''SO'' As AppId, 2 As [Type], '''' As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''19e5ccc8-baeb-4dee-8110-5b566fb118d7''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''19e5ccc8-baeb-4dee-8110-5b566fb118d7'' As ID, N''SO Transaction Order Serial'' As [Name], N''SO'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''d9125f2e-e9cf-48e1-be3b-fbcd75fa96e5''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''d9125f2e-e9cf-48e1-be3b-fbcd75fa96e5'' As ID, N''SO Transaction Payment'' As [Name], N''SO'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''7b918194-11e4-49cc-9934-1ff9ba5d2aea''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''7b918194-11e4-49cc-9934-1ff9ba5d2aea'' As ID, N''SO Update Contract Pricing'' As [Name], N''SO'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''72a160c7-962d-43e0-80e5-f8a55675be91''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''72a160c7-962d-43e0-80e5-f8a55675be91'' As ID, N''WM Adjustment'' As [Name], N''WM'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''b6712265-f759-4ace-9b53-64e0e15cbf92''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''b6712265-f759-4ace-9b53-64e0e15cbf92'' As ID, N''WM Adjustment Serial List'' As [Name], N''WM'' As AppId, 2 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''7893caaa-0b68-4792-919d-52e08a0c9436''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''7893caaa-0b68-4792-919d-52e08a0c9436'' As ID, N''WM Adjustments Batch'' As [Name], N''WM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''37fa9163-a68b-4a8e-901b-e30dee2c3d63''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''37fa9163-a68b-4a8e-901b-e30dee2c3d63'' As ID, N''WM Bin'' As [Name], N''WM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''e5d062aa-48f6-457a-a591-38b99238ced9''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''e5d062aa-48f6-457a-a591-38b99238ced9'' As ID, N''WM Bin Types'' As [Name], N''WM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''50802ece-0c7e-4077-9f8a-cc070215af95''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''50802ece-0c7e-4077-9f8a-cc070215af95'' As ID, N''WM Containers'' As [Name], N''WM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''80db8c35-e296-473b-80ff-7b47c082d55a''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''80db8c35-e296-473b-80ff-7b47c082d55a'' As ID, N''WM Location Transfer'' As [Name], N''WM'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''b546cb1f-b270-4748-9fc4-02e1575f069d''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''b546cb1f-b270-4748-9fc4-02e1575f069d'' As ID, N''WM Move Quantities'' As [Name], N''WM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''89e5a049-4dfc-4277-83d3-32c941bf977b''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''89e5a049-4dfc-4277-83d3-32c941bf977b'' As ID, N''WM Pack Order'' As [Name], N''WM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''cdfc27ad-7978-480b-99f1-168a112b1397''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''cdfc27ad-7978-480b-99f1-168a112b1397'' As ID, N''WM Pack Order Detail'' As [Name], N''WM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''094db268-4855-40df-8181-91e2c54b2ad3''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''094db268-4855-40df-8181-91e2c54b2ad3'' As ID, N''WM Physical Count Batch'' As [Name], N''WM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''b4f6f157-9ded-47cb-a96a-2f598c8fd533''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''b4f6f157-9ded-47cb-a96a-2f598c8fd533'' As ID, N''WM Physical Count Entry'' As [Name], N''WM'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''4cc14ee6-f646-46a9-8d2c-ce2f71aaa5d3''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''4cc14ee6-f646-46a9-8d2c-ce2f71aaa5d3'' As ID, N''WM Pick'' As [Name], N''WM'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''c877ffee-fdf2-4f75-9835-0b43a0dc3658''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''c877ffee-fdf2-4f75-9835-0b43a0dc3658'' As ID, N''WM Pick Detail'' As [Name], N''WM'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''e0fb977f-31eb-4e50-bd1e-508047fb7067''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''e0fb977f-31eb-4e50-bd1e-508047fb7067'' As ID, N''WM Pick SubDetail'' As [Name], N''WM'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''80bfffe4-1438-4ede-b7a2-5bf00ab2f7e0''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''80bfffe4-1438-4ede-b7a2-5bf00ab2f7e0'' As ID, N''WM Put Away'' As [Name], N''WM'' As AppId, 1 As [Type], NULL As Notes, 5 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''1b0ebfc8-a19c-43f4-bec9-9452340b2e2e''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''1b0ebfc8-a19c-43f4-bec9-9452340b2e2e'' As ID, N''WM Put Away Detail'' As [Name], N''WM'' As AppId, 3 As [Type], NULL As Notes, 13 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''64990665-1ec4-4c50-b3cd-bfc661f99895''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''64990665-1ec4-4c50-b3cd-bfc661f99895'' As ID, N''WM Put Away Serial'' As [Name], N''WM'' As AppId, 3 As [Type], NULL As Notes, 5 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''b6fc751e-24ee-44be-9187-64ee781e389a''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''b6fc751e-24ee-44be-9187-64ee781e389a'' As ID, N''WM Receive'' As [Name], N''WM'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''5942365d-04d1-4186-9017-06705c7c802e''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''5942365d-04d1-4186-9017-06705c7c802e'' As ID, N''WM Receive Detail'' As [Name], N''WM'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''45614b7c-d007-47a9-9aef-8082bc28a8d6''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''45614b7c-d007-47a9-9aef-8082bc28a8d6'' As ID, N''WM Release Items Available'' As [Name], N''WM'' As AppId, 3 As [Type], NULL As Notes, 1 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''662462eb-bad6-46d3-8292-55a8f423fedf''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''662462eb-bad6-46d3-8292-55a8f423fedf'' As ID, N''WM Release Items Pick'' As [Name], N''WM'' As AppId, 3 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''20201e44-04cd-4b08-936e-af018a24a77a''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''20201e44-04cd-4b08-936e-af018a24a77a'' As ID, N''WM Transfers Batch'' As [Name], N''WM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID
DELETE FROM ApiFunctionHeader WHERE ID = N''299d8d5b-19db-4cbf-88f2-b1c4094b1f36''
INSERT INTO ApiFunctionHeader (ID, [Name], AppId, [Type], Notes, Scope, OverrideID)
SELECT N''299d8d5b-19db-4cbf-88f2-b1c4094b1f36'' As ID, N''WM Zones'' As [Name], N''WM'' As AppId, 1 As [Type], NULL As Notes, 15 As Scope, NULL As OverrideID


	USE [' + DB_NAME() + '];
'

	EXEC (@sql)
	FETCH NEXT FROM dbUpdate INTO @dbName
END

CLOSE dbUpdate
DEALLOCATE dbUpdate

DROP TABLE #DatabaseList;
