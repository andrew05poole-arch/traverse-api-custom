DECLARE @DBFrom sysname,@DBTo sysname, @sql nvarchar(max), @sql1 nvarchar(max), @sql2 nvarchar(max), @sql3 nvarchar(max)
SET @DBFrom= 'TraverseApi'-- Change this DB name according to your Old travserse API database name
SET @DBTo = 'SYSWeb'-- Change this to your new Sys DB name

set @sql='DELETE FROM ['+ @DBTo+ '].dbo.tblSmApiFunctionHeader 
INSERT INTO['+ @DBTo+ '].dbo.tblSmApiFunctionHeader(ID,Name,AppId,Type,Notes,Scope,OverrideID,QueryTableName)  SELECT ID,Name,AppId,Type,Notes,Scope,OverrideID,QueryTableName FROM ['+ @DBFrom+ '].dbo.ApiFunctionHeader

Delete FROM ['+ @DBTo+ '].dbo.tblSmApiFunctionSchema

set identity_insert ['+ @DBTo+ '].dbo.tblSmApiFunctionSchema on
INSERT INTO ['+ @DBTo+ '].dbo.tblSmApiFunctionSchema(ID,SeqNum,FunctionID,TravFieldName,ApiFieldName,ValueTranslation,Notes,FieldSetting,ChildFunctionID,QueryColumnName) 
SELECT ID,SeqNum,FunctionID,TravFieldName,ApiFieldName,ValueTranslation,Notes,FieldSetting,ChildFunctionID,QueryColumnName FROM ['+ @DBFrom+ '].dbo.ApiFunctionSchema
set identity_insert ['+ @DBTo+ '].dbo.tblSmApiUserFunctionSchema  off


Delete FROM ['+ @DBTo+ '].dbo.tblSmApiInfo
INSERT INTO['+ @DBTo+ '].dbo.tblSmApiInfo(ID,SysDb,Version,AuthorizeTimeout,AccessExpireHours,RefreshExpireDays,IsDebugLocalEnv,IgnoreInvalidField,UseMsgSecurity,MsgSharedKey,MsgEncryption)  SELECT ID,SysDb,Version,AuthorizeTimeout,AccessExpireHours,RefreshExpireDays,IsDebugLocalEnv,IgnoreInvalidField,UseMsgSecurity,MsgSharedKey,MsgEncryption FROM ['+ @DBFrom+ '].dbo.ApiInfo

Delete FROM ['+ @DBTo+ '].dbo.tblSmApiUser
INSERT INTO['+ @DBTo+ '].dbo.tblSmApiUser(ID,EmailAddress,Password,Name,Role,Status,ResetPassword,AdditionalInfo,ExpirationDate,ClientID,ClientSecret,DateCreated,DateModified,ModifiedBy)  SELECT ID,EmailAddress,Password,Name,Role,Status,ResetPassword,AdditionalInfo,ExpirationDate,ClientID,ClientSecret,DateCreated,DateModified,ModifiedBy FROM ['+ @DBFrom+ '].dbo.ApiUser

Delete FROM ['+ @DBTo+ '].dbo.tblSmApiUserFunctionAccess
INSERT INTO['+ @DBTo+ '].dbo.tblSmApiUserFunctionAccess(UserFunctionID,AccessCount,FirstAccessTime,LastAccessTime,LastAccessMethod)  SELECT UserFunctionID,AccessCount,FirstAccessTime,LastAccessTime,LastAccessMethod FROM ['+ @DBFrom+ '].dbo.ApiUserFunctionAccess

Delete FROM ['+ @DBTo+ '].dbo.tblSmApiUserToken
INSERT INTO['+ @DBTo+ '].dbo.tblSmApiUserToken(UserID,RefreshToken,IssueTime,ExpireTime)  SELECT UserID,RefreshToken,IssueTime,ExpireTime FROM ['+ @DBFrom+ '].dbo.ApiUserToken

'
exec (@sql)

set @sql1='
Delete FROM ['+ @DBTo+ '].dbo.tblSmApiUserFunction
set identity_insert ['+ @DBTo+ '].dbo.tblSmApiUserFunction on
INSERT INTO['+ @DBTo+ '].dbo.tblSmApiUserFunction(ID,UserID,FunctionID,AccessExpireDate,DateCreated,DateModified,ModifiedBy)  SELECT ID,UserID,FunctionID,AccessExpireDate,DateCreated,DateModified,ModifiedBy FROM ['+ @DBFrom+ '].dbo.ApiUserFunction
set identity_insert ['+ @DBTo+ '].dbo.tblSmApiUserFunction  off
'
exec (@sql1)

set @sql2='
Delete FROM ['+ @DBTo+ '].dbo.tblSmApiUserFunctionComp
set identity_insert ['+ @DBTo+ '].dbo.tblSmApiUserFunctionComp on
INSERT INTO['+ @DBTo+ '].dbo.tblSmApiUserFunctionComp(ID,UserFunctionID,CompID,Scope,Filter,DisplayFilter,DateCreated,DateModified,ModifiedBy)  SELECT ID,UserFunctionID,CompID,Scope,Filter,DisplayFilter,DateCreated,DateModified,ModifiedBy FROM ['+ @DBFrom+ '].dbo.ApiUserFunctionComp
set identity_insert ['+ @DBTo+ '].dbo.tblSmApiUserFunctionComp  off
'
exec (@sql2)

set @sql3='
Delete FROM ['+ @DBTo+ '].dbo.tblSmApiUserFunctionSchema
set identity_insert ['+ @DBTo+ '].dbo.tblSmApiUserFunctionSchema on
INSERT INTO['+ @DBTo+ '].dbo.tblSmApiUserFunctionSchema(ID,UserFunctionID,FieldType,Notes,FunctionSchemaID,CustomFieldName,ApiFieldName,Hidden,DefaultValue,DateCreated,DateModified,ModifiedBy)  SELECT ID,UserFunctionID,FieldType,Notes,FunctionSchemaID,CustomFieldName,ApiFieldName,Hidden,DefaultValue,DateCreated,DateModified,ModifiedBy FROM ['+ @DBFrom+ '].dbo.ApiUserFunctionSchema
set identity_insert ['+ @DBTo+ '].dbo.tblSmApiUserFunctionSchema  off
'
exec (@sql3)
