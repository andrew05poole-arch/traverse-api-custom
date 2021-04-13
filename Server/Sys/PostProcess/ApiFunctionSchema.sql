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
IF NOT EXISTS (SELECT * FROM sys.tables WHERE [object_id] = OBJECT_ID(N''[dbo].[ApiFunctionSchema]''))
BEGIN
	CREATE TABLE [dbo].[ApiFunctionSchema] (
		[ID] [bigint] NOT NULL IDENTITY(1, 1),		--Counter
		[SeqNum] [int] NULL,
		[FunctionID] [uniqueidentifier] NULL,		--ID from Function
		[TravFieldName] [nvarchar](128) NULL,		--Property name in class (not the SQL field name)
		[ApiFieldName] [nvarchar](128) NULL,		--Usage name in Traverse API
		[ValueTranslation] [nvarchar](max) NULL,	--enumerations
		[Notes] [nvarchar](max) NULL,
		[FieldSetting] [tinyint] NULL,				--1 = Required; 2 = ReadOnly; 4 = WriteOnly
		[ChildFunctionID] [uniqueidentifier] NULL,	--Option for making use of summary calls; such as sales order transaction including line items and extended details
		[QueryColumnName] [nvarchar](128) NULL,
		[ts] [timestamp] NULL,
		CONSTRAINT [PK_ApiFunctionSchema] PRIMARY KEY CLUSTERED (
			[ID] ASC
		) ON [PRIMARY]
	) ON [PRIMARY];
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

DELETE FROM ApiFunctionSchema WHERE FunctionID = N''765ae38b-af2c-4529-b445-a914ca369a4b''
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 10 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''TransId'' As TravFieldName, N''transaction_id'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 35 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 20 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''SoldToId'' As TravFieldName, N''sold_to_id'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 19 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 30 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''TransDate'' As TravFieldName, N''transaction_date'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 40 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''TransType'' As TravFieldName, N''transaction_type'' As ApiFieldName, N''<?xml version="1.0" encoding="utf-16"?>
<ArrayOfApiValueTranslate xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <ApiValueTranslate>
    <Key>1</Key>
    <Value>Invoice</Value>
  </ApiValueTranslate>
  <ApiValueTranslate>
    <Key>2</Key>
    <Value>Price Quote</Value>
  </ApiValueTranslate>
  <ApiValueTranslate>
    <Key>3</Key>
    <Value>Backordered</Value>
  </ApiValueTranslate>
  <ApiValueTranslate>
    <Key>4</Key>
    <Value>Verified</Value>
  </ApiValueTranslate>
  <ApiValueTranslate>
    <Key>5</Key>
    <Value>Picked</Value>
  </ApiValueTranslate>
  <ApiValueTranslate>
    <Key>9</Key>
    <Value>New</Value>
  </ApiValueTranslate>
  <ApiValueTranslate>
    <Key>-1</Key>
    <Value>Credit Memo</Value>
  </ApiValueTranslate>
  <ApiValueTranslate>
    <Key>-2</Key>
    <Value>RMA</Value>
  </ApiValueTranslate>
</ArrayOfApiValueTranslate>'' As ValueTranslation, NULL As Notes, 3 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 50 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''CustPONum'' As TravFieldName, N''customer_po_number'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 60 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''PODate'' As TravFieldName, N''po_date'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 70 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''Notes'' As TravFieldName, N''notes'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 80 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''BatchId'' As TravFieldName, N''batch_code'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 90 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''LocId'' As TravFieldName, N''location_id'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 100 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''ReqShipDate'' As TravFieldName, N''req_ship_date'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 110 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''GLPeriod'' As TravFieldName, N''fiscal_period'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 120 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''FiscalYear'' As TravFieldName, N''fiscal_year'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 130 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''CurrencyId'' As TravFieldName, N''currency_id'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 3 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 140 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''OrgInvcNum'' As TravFieldName, N''original_invoice_number'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 150 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''CustId'' As TravFieldName, N''bill_to_id'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 160 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''CustLevel'' As TravFieldName, N''customer_level'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 170 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''TermsCode'' As TravFieldName, N''terms_code'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 180 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''DistCode'' As TravFieldName, N''distribution_code'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 190 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''InvcNum'' As TravFieldName, N''invoice_number'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 200 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''InvcDate'' As TravFieldName, N''invoice_date'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 210 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''Rep1Id'' As TravFieldName, N''sales_rep_id_1'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 220 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''Rep1Pct'' As TravFieldName, N''sales_rep_id_1_percent'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 230 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''Rep1CommRate'' As TravFieldName, N''sales_rep_id_1_rate'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 240 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''Rep2Id'' As TravFieldName, N''sales_rep_id_2_id'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 250 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''Rep2Pct'' As TravFieldName, N''sales_rep_id_2_percent'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 260 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''Rep2CommRate'' As TravFieldName, N''sales_rep_id_2_rate'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 270 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''ShipToId'' As TravFieldName, N''ship_to_id'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 280 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''ShipVia'' As TravFieldName, N''ship_via'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 290 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''ShipMethod'' As TravFieldName, N''ship_method'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 300 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''ShipNum'' As TravFieldName, N''ship_number'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 310 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''ShipToName'' As TravFieldName, N''ship_to_name'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 320 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''ShipToAttn'' As TravFieldName, N''ship_to_attention'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 330 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''ShipToAddr1'' As TravFieldName, N''ship_to_address_1'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 340 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''ShipToAddr2'' As TravFieldName, N''ship_to_address_2'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 350 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''ShipToCity'' As TravFieldName, N''ship_to_city'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 360 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''ShipToRegion'' As TravFieldName, N''ship_to_region'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 370 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''ShipToCountry'' As TravFieldName, N''ship_to_country'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 380 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''ShipToPostalCode'' As TravFieldName, N''ship_to_postal_code'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 390 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''ActShipDate'' As TravFieldName, N''actual_ship_date'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 400 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''LineItemList'' As TravFieldName, N''order_line_list'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 410 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''TaxGrpId'' As TravFieldName, N''tax_group_id'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 420 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''TaxableYN'' As TravFieldName, N''taxable'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 430 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''TaxClassAdj'' As TravFieldName, N''tax_class_adjustment'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 440 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''FreightFgn'' As TravFieldName, N''freight'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 450 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''TaxClassFreight'' As TravFieldName, N''tax_class_freight'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 460 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''TaxLocAdj'' As TravFieldName, N''tax_location_adjustment'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 470 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''MiscFgn'' As TravFieldName, N''misc'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 480 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''TaxClassMisc'' As TravFieldName, N''tax_class_misc'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 490 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''NetSalesTaxFgn'' As TravFieldName, N''sales_tax'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 500 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''TaxableSalesFgn'' As TravFieldName, N''taxable_sales'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 1 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 510 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''NonTaxableSalesFgn'' As TravFieldName, N''non_taxable_sales'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 1 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 520 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''PaymentList'' As TravFieldName, N''payment_list'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, N''d9125f2e-e9cf-48e1-be3b-fbcd75fa96e5'' As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 530 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''TotPostedPmtAmt'' As TravFieldName, N''posted_payments'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 1 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 540 As SeqNum, N''765ae38b-af2c-4529-b445-a914ca369a4b'' As FunctionID, N''TotPostedInvoiceAmt'' As TravFieldName, N''posted_invoices'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 1 As FieldSetting, NULL As ChildFunctionID
DELETE FROM ApiFunctionSchema WHERE FunctionID = N''c16315f8-8f21-419d-90c1-3176c35dddc6''
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 10 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''EntryNum'' As TravFieldName, N''entry_number'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 20 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''ItemId'' As TravFieldName, N''item_id'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 30 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''Description'' As TravFieldName, N''description'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 40 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''CustomerPartNumber'' As TravFieldName, N''customer_part_no'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 50 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''AddnlDescr'' As TravFieldName, N''additional_description'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 60 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''LocId'' As TravFieldName, N''location_id'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 70 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''TotQtyOrdSell'' As TravFieldName, N''quantity_ordered'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 80 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''UnitsSell'' As TravFieldName, N''units'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 90 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''QtyShipSell'' As TravFieldName, N''quantity_shipped'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 100 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''QtyBackordSell'' As TravFieldName, N''quantity_backordered'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 110 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''ReqShipDate'' As TravFieldName, N''req_ship_date'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 120 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''UnitPriceSellBasisFgn'' As TravFieldName, N''unit_price'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 130 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''PriceExtFgn'' As TravFieldName, N''extended_price'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 1 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 140 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''ResCode'' As TravFieldName, N''reason_code'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 150 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''AcctCode'' As TravFieldName, N''account_code'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 160 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''GLAcctInv'' As TravFieldName, N''inventory_gl_account'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 170 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''GLAcctSales'' As TravFieldName, N''sales_gl_account'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 180 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''GLAcctCOGS'' As TravFieldName, N''cogs_gl_account'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 190 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''CatId'' As TravFieldName, N''sales_category'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 200 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''TaxClass'' As TravFieldName, N''tax_class'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 210 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''PromoId'' As TravFieldName, N''promo_id'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 220 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''PriceId'' As TravFieldName, N''price_id'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 230 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''PriceAdjType'' As TravFieldName, N''discount_type'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 240 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''PriceAdjPct'' As TravFieldName, N''discount_percentage'' As ApiFieldName, N''<?xml version="1.0" encoding="utf-16"?>
<ArrayOfApiValueTranslate xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <ApiValueTranslate>
    <Key>0</Key>
    <Value>Amount</Value>
  </ApiValueTranslate>
  <ApiValueTranslate>
    <Key>1</Key>
    <Value>Percentage</Value>
  </ApiValueTranslate>
</ArrayOfApiValueTranslate>'' As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 250 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''PriceAdjAmtFgn'' As TravFieldName, N''discount_amount'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 260 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''Rep1Id'' As TravFieldName, N''sales_rep_id_1'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 270 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''Rep1Pct'' As TravFieldName, N''sales_rep1_percent'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 280 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''Rep1CommRate'' As TravFieldName, N''sales_rep1_rate'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 290 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''Rep2Id'' As TravFieldName, N''sales_rep_id_2'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 300 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''Rep2Pct'' As TravFieldName, N''sales_rep2_percent'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 310 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''Rep2CommRate'' As TravFieldName, N''sales_rep2_rate'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 320 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''UnitCostSellFgn'' As TravFieldName, N''unit_cost'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 330 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''CostExtFgn'' As TravFieldName, N''extended_cost'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 1 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 340 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''Status'' As TravFieldName, N''status'' As ApiFieldName, N''<?xml version="1.0" encoding="utf-16"?>
<ArrayOfApiValueTranslate xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <ApiValueTranslate>
    <Key>0</Key>
    <Value>Open</Value>
  </ApiValueTranslate>
  <ApiValueTranslate>
    <Key>1</Key>
    <Value>Completed</Value>
  </ApiValueTranslate>
</ArrayOfApiValueTranslate>'' As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 350 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''ExtendedList'' As TravFieldName, N''extended_list'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, N''e59b69e9-fce8-47ad-bfec-c4bf934b6e63'' As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 360 As SeqNum, N''c16315f8-8f21-419d-90c1-3176c35dddc6'' As FunctionID, N''SerialList'' As TravFieldName, N''serial_list'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, N''19e5ccc8-baeb-4dee-8110-5b566fb118d7'' As ChildFunctionID
DELETE FROM ApiFunctionSchema WHERE FunctionID = N''19e5ccc8-baeb-4dee-8110-5b566fb118d7''
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 10 As SeqNum, N''19e5ccc8-baeb-4dee-8110-5b566fb118d7'' As FunctionID, N''SerNum'' As TravFieldName, N''serial_number'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 3 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 20 As SeqNum, N''19e5ccc8-baeb-4dee-8110-5b566fb118d7'' As FunctionID, N''LotNum'' As TravFieldName, N''lot_number'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 3 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 30 As SeqNum, N''19e5ccc8-baeb-4dee-8110-5b566fb118d7'' As FunctionID, N''CostUnitFgn'' As TravFieldName, N''unit_cost'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 40 As SeqNum, N''19e5ccc8-baeb-4dee-8110-5b566fb118d7'' As FunctionID, N''PriceUnitFgn'' As TravFieldName, N''unit_price'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 50 As SeqNum, N''19e5ccc8-baeb-4dee-8110-5b566fb118d7'' As FunctionID, N''Comment'' As TravFieldName, N''comment'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 60 As SeqNum, N''19e5ccc8-baeb-4dee-8110-5b566fb118d7'' As FunctionID, N''ExtLocA'' As TravFieldName, N''bin'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
DELETE FROM ApiFunctionSchema WHERE FunctionID = N''e59b69e9-fce8-47ad-bfec-c4bf934b6e63''
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 10 As SeqNum, N''e59b69e9-fce8-47ad-bfec-c4bf934b6e63'' As FunctionID, N''SeqNum'' As TravFieldName, N''seq_num'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 35 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 20 As SeqNum, N''e59b69e9-fce8-47ad-bfec-c4bf934b6e63'' As FunctionID, N''LotNum'' As TravFieldName, N''lot_number'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 3 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 20 As SeqNum, N''e59b69e9-fce8-47ad-bfec-c4bf934b6e63'' As FunctionID, N''LotNumber'' As TravFieldName, N''lot_number'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 3 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 30 As SeqNum, N''e59b69e9-fce8-47ad-bfec-c4bf934b6e63'' As FunctionID, N''ExtLocA'' As TravFieldName, N''bin'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 40 As SeqNum, N''e59b69e9-fce8-47ad-bfec-c4bf934b6e63'' As FunctionID, N''ExtLocB'' As TravFieldName, N''container'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 50 As SeqNum, N''e59b69e9-fce8-47ad-bfec-c4bf934b6e63'' As FunctionID, N''QtyOrder'' As TravFieldName, N''quantity_ordered'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 60 As SeqNum, N''e59b69e9-fce8-47ad-bfec-c4bf934b6e63'' As FunctionID, N''QtyFilled'' As TravFieldName, N''quantity_filled'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 70 As SeqNum, N''e59b69e9-fce8-47ad-bfec-c4bf934b6e63'' As FunctionID, N''CostUnitFgn'' As TravFieldName, N''unit_cost'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID
INSERT INTO ApiFunctionSchema (SeqNum, FunctionID, [TravFieldName], ApiFieldName, ValueTranslation, Notes, FieldSetting, ChildFunctionID)
SELECT 80 As SeqNum, N''e59b69e9-fce8-47ad-bfec-c4bf934b6e63'' As FunctionID, N''Cmnt'' As TravFieldName, N''comment'' As ApiFieldName, NULL As ValueTranslation, NULL As Notes, 7 As FieldSetting, NULL As ChildFunctionID


	USE [' + DB_NAME() + '];
'

	EXEC (@sql)
	FETCH NEXT FROM dbUpdate INTO @dbName
END

CLOSE dbUpdate
DEALLOCATE dbUpdate

DROP TABLE #DatabaseList;
