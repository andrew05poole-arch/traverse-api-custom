DECLARE @DBFrom sysname,@DBTo sysname, @sql1 nvarchar(max), @sql2 nvarchar(max)
SET @DBFrom= 'TraverseApi'-- Change this DB name according to your Old travserse API database name
SET @DBTo = 'SYSGLB'-- Change this to your new Sys DB name

set @sql1='
IF NOT EXISTS(SELECT * FROM ['+ @DBTo+ '].dbo.tblSysUpdateActivity WHERE ActivityId = ''f4eeac83-878d-42fd-8bf8-fc32886f5b6e'')
BEGIN 
	INSERT INTO['+ @DBTo+ '].dbo.tblSmApiFunctionHeader(ID,Name,AppId,Type,Notes,Scope,OverrideID,QueryTableName)  
	SELECT ID,Name,AppId,Type,Notes,Scope,OverrideID,QueryTableName FROM ['+ @DBFrom+ '].dbo.ApiFunctionHeader WHERE ID NOT IN (SELECT ID FROM ['+ @DBTo+ '].dbo.tblSmApiFunctionHeader)
	
	set identity_insert ['+ @DBTo+ '].dbo.tblSmApiFunctionSchema on
	INSERT INTO ['+ @DBTo+ '].dbo.tblSmApiFunctionSchema(ID,SeqNum,FunctionID,TravFieldName,ApiFieldName,ValueTranslation,Notes,FieldSetting,ChildFunctionID,QueryColumnName) 
	SELECT a.ID,a.SeqNum,a.FunctionID,a.TravFieldName,a.ApiFieldName,a.ValueTranslation,a.Notes,a.FieldSetting,a.ChildFunctionID,a.QueryColumnName FROM ['+ @DBFrom+ '].dbo.ApiFunctionSchema a 
		LEFT JOIN ['+ @DBTo+ '].dbo.tblSmApiFunctionSchema b ON a.FunctionID = b.FunctionID AND a.TravFieldName = b.TravFieldName 
	WHERE b.ID IS NULL
	set identity_insert ['+ @DBTo+ '].dbo.tblSmApiUserFunctionSchema  off

	INSERT INTO ['+ @DBTo+ '].dbo.tblSysUpdateActivity([ActivityId], [Description])
	VALUES (''f4eeac83-878d-42fd-8bf8-fc32886f5b6e'', ''Migrate API metadata from '+ @DBFrom+ ' to '+ @DBTo+ '.'') 
END
'
exec (@sql1)

set @sql2='
IF NOT EXISTS(SELECT * FROM ['+ @DBTo+ '].dbo.tblSysUpdateActivity WHERE ActivityId = ''efc6a75e-cf0b-4e1c-b7a6-18497729051a'')
BEGIN 
	Delete FROM ['+ @DBTo+ '].dbo.tblSmApiInfo
	INSERT INTO['+ @DBTo+ '].dbo.tblSmApiInfo(ID,SysDb,Version,AuthorizeTimeout,AccessExpireHours,RefreshExpireDays,IsDebugLocalEnv,IgnoreInvalidField,UseMsgSecurity,MsgSharedKey,MsgEncryption)  SELECT ID,SysDb,Version,AuthorizeTimeout,AccessExpireHours,RefreshExpireDays,IsDebugLocalEnv,IgnoreInvalidField,UseMsgSecurity,MsgSharedKey,MsgEncryption FROM ['+ @DBFrom+ '].dbo.ApiInfo
	
	Delete FROM ['+ @DBTo+ '].dbo.tblSmApiUser
	INSERT INTO['+ @DBTo+ '].dbo.tblSmApiUser(ID,EmailAddress,Password,Name,Role,Status,ResetPassword,AdditionalInfo,ExpirationDate,ClientID,ClientSecret,DateCreated,DateModified,ModifiedBy)  SELECT ID,EmailAddress,Password,Name,Role,Status,ResetPassword,AdditionalInfo,ExpirationDate,ClientID,ClientSecret,DateCreated,DateModified,ModifiedBy FROM ['+ @DBFrom+ '].dbo.ApiUser
	
	Delete FROM ['+ @DBTo+ '].dbo.tblSmApiUserFunctionAccess
	INSERT INTO['+ @DBTo+ '].dbo.tblSmApiUserFunctionAccess(UserFunctionID,AccessCount,FirstAccessTime,LastAccessTime,LastAccessMethod)  SELECT UserFunctionID,AccessCount,FirstAccessTime,LastAccessTime,LastAccessMethod FROM ['+ @DBFrom+ '].dbo.ApiUserFunctionAccess
	
	Delete FROM ['+ @DBTo+ '].dbo.tblSmApiUserToken
	INSERT INTO['+ @DBTo+ '].dbo.tblSmApiUserToken(UserID,RefreshToken,IssueTime,ExpireTime)  SELECT UserID,RefreshToken,IssueTime,ExpireTime FROM ['+ @DBFrom+ '].dbo.ApiUserToken
	
	Delete FROM ['+ @DBTo+ '].dbo.tblSmApiUserFunction
	set identity_insert ['+ @DBTo+ '].dbo.tblSmApiUserFunction on
	INSERT INTO['+ @DBTo+ '].dbo.tblSmApiUserFunction(ID,UserID,FunctionID,AccessExpireDate,DateCreated,DateModified,ModifiedBy)  SELECT ID,UserID,FunctionID,AccessExpireDate,DateCreated,DateModified,ModifiedBy FROM ['+ @DBFrom+ '].dbo.ApiUserFunction
	set identity_insert ['+ @DBTo+ '].dbo.tblSmApiUserFunction  off
	
	Delete FROM ['+ @DBTo+ '].dbo.tblSmApiUserFunctionComp
	INSERT INTO['+ @DBTo+ '].dbo.tblSmApiUserFunctionComp(UserFunctionID,CompID,Scope,Filter,DisplayFilter,DateCreated,DateModified,ModifiedBy)  SELECT UserFunctionID,CompID,Scope,Filter,DisplayFilter,DateCreated,DateModified,ModifiedBy FROM ['+ @DBFrom+ '].dbo.ApiUserFunctionComp

	Delete FROM ['+ @DBTo+ '].dbo.tblSmApiUserFunctionSchema
	INSERT INTO['+ @DBTo+ '].dbo.tblSmApiUserFunctionSchema(UserFunctionID,FieldType,Notes,FunctionSchemaID,CustomFieldName,ApiFieldName,Hidden,DefaultValue,DateCreated,DateModified,ModifiedBy)  SELECT UserFunctionID,FieldType,Notes,FunctionSchemaID,CustomFieldName,ApiFieldName,Hidden,DefaultValue,DateCreated,DateModified,ModifiedBy FROM ['+ @DBFrom+ '].dbo.ApiUserFunctionSchema
	
	INSERT INTO ['+ @DBTo+ '].dbo.tblSysUpdateActivity([ActivityId], [Description])
	VALUES (''efc6a75e-cf0b-4e1c-b7a6-18497729051a'', ''Migrate API user data from '+ @DBFrom+ ' to '+ @DBTo+ '.'') END
'
exec (@sql2)
