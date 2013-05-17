-- Script for migration from previous version to the new tables

-- Clear tables to avoid duplicates (in case the migration script was interrupted before)
truncate table [dbo].[UserGroupMembership]
delete from [dbo].[UserGroup] -- cannot truncate due to FK references
delete from [dbo].[User] -- cannot truncate due to FK references

truncate table [dbo].[UserData]

insert into [dbo].[User]
	([Wiki]
	,[Username]
	,[PasswordHash]
	,[DisplayName]
	,[Email]
	,[Active]
	,[DateTime])
select 
	'root' as [Wiki]
	,[Username]
	,[PasswordHash]
	,[DisplayName]
	,[Email]
	,[Active]
	,[DateTime]
from  [dbo].[User_v3]

insert into [dbo].[UserGroup]
	([Wiki]
	,[Name]
	,[Description])
select 
	'root' as [Wiki]
	,[Name]
	,[Description]
from [dbo].[UserGroup_v3]

insert into [dbo].[UserGroupMembership]
	([Wiki]
	,[User]
	,[UserGroup])
select 
	'root' as [Wiki]
	,[User]
	,[UserGroup]
from [dbo].[UserGroupMembership_v3]

insert into [dbo].[UserData]
	([Wiki]
	,[User]
	,[Key]
	,[Data])
select 
	'root' as [Wiki]
	,[User]
	,[Key]
	,[Data]
from [dbo].[UserData_v3]

update [Version] set [Version] = 4000 where [Component] = 'Users'