-- Script for migration from previous version to the new tables

-- Clear tables to avoid duplicates (in case the migration script was interrupted before)
truncate table [dbo].[File]
truncate table [dbo].[Attachment]
delete from [dbo].[Directory] -- cannot truncate due to FK references

insert into [dbo].[Directory]
	([Wiki]
	,[FullPath]
	,[Parent])
select 
	'root' as [Wiki]
	,[FullPath]
	,[Parent]
from [dbo].[Directory_v3]


insert into [dbo].[File]
	([Wiki]
	,[Name]
	,[Directory]
	,[Size]
	,[LastModified]
	,[Data])
select 
	'root' as [Wiki]
	,[Name]
	,[Directory]
	,[Size]
	,[LastModified]
	,[Data]
from [dbo].[File_v3]


insert into [dbo].[Attachment]
	([Wiki]
	,[Name]
	,[Page]
	,[Size]
	,[LastModified]
	,[Data])
select 
	'root' as [Wiki]
	,[Name]
	,[Page]
	,[Size]
	,[LastModified]
	,[Data]
from [dbo].[Attachment_v3]

update [Version] set [Version] = 4000 where [Component] = 'Files'