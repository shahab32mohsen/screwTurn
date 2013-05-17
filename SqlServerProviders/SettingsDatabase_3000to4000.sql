-- Script for migration from previous version to the new tables

-- Clear tables to avoid duplicates (in case the migration script was interrupted before)
truncate table [dbo].[Setting]
truncate table [dbo].[MetaDataItem]
truncate table [dbo].[RecentChange]
truncate table [dbo].[PluginStatus]
truncate table [dbo].[OutgoingLink]
truncate table [dbo].[AclEntry]

insert into [dbo].[Setting]
	([Wiki]
	,[Name]
	,[Value])
select 
	'root' as [Wiki]
	,[Name]
	,[Value]
from [dbo].[Setting_v3]

insert into [dbo].[MetaDataItem]
	([Wiki]
	,[Name]
	,[Tag]
	,[Data])
select 
	'root' as [Wiki]
	,[Name]
	,[Tag]
	,[Data]
from [dbo].[MetaDataItem_v3]

insert into [dbo].[RecentChange]
	([Wiki]
	,[Page]
	,[Title]
	,[MessageSubject]
	,[DateTime]
	,[User]
	,[Change]
	,[Description])
select 
	'root' as [Wiki]
	,[Page]
	,[Title]
	,[MessageSubject]
	,[DateTime]
	,[User]
	,[Change]
	,[Description]
from [dbo].[RecentChange_v3]

insert into [dbo].[PluginStatus]
	([Wiki]
	,[Name]
	,[Enabled]
	,[Configuration])
select 
	'root' as [Wiki]
	,[Name]
	,[Enabled]
	,[Configuration]
from [dbo].[PluginStatus_v3]

insert into [dbo].[OutgoingLink]
	([Wiki]
	,[Source]
	,[Destination])
select 
	'root' as [Wiki]
	,[Source]
	,[Destination]
from [dbo].[OutgoingLink_v3]

insert into [dbo].[AclEntry]
	([Wiki]
	,[Resource]
	,[Action]
	,[Subject]
	,[Value])
select 
	'root' as [Wiki]
	,[Resource]
	,[Action]
	,[Subject]
	,[Value]
from [dbo].[AclEntry_v3]

-- Changes to the themes info
update [Setting] set [Value] = 'standard|' + [Value] where [Name] like 'Theme%' and CHARINDEX('|', value, 0) <= 0

update [Version] set [Version] = 4000 where [Component] = 'Settings'