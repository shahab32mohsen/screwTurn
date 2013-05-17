-- Script for migration from previous version to the new tables

-- Clear tables to avoid duplicates (in case the migration script was interrupted before)
truncate table [dbo].[CategoryBinding]
truncate table [dbo].[PageKeyword]
truncate table [dbo].[Message]
truncate table [dbo].[NavigationPath]
truncate table [dbo].[Snippet]
truncate table [dbo].[ContentTemplate]
-- cannot truncate due to FK references
delete from [dbo].[Category] 
delete from [dbo].[PageContent]
delete from [dbo].[Namespace]

insert into [dbo].[Namespace]
	([Wiki]
	,[Name]
	,[DefaultPage])
select 
	'root' as [Wiki]
	,[Name]
	,[DefaultPage]
from [dbo].[Namespace_v3]

insert into [dbo].[Category]
	([Wiki]
	,[Name]
	,[Namespace])
select 
	'root' as [Wiki]
	,[Name]
	,[Namespace]
from [dbo].[Category_v3]

insert into [dbo].[PageContent]
	([Wiki]
	,[Name]
	,[CreationDateTime]
	,[Namespace]
	,[Revision]
	,[Title]
	,[User]
	,[LastModified]
	,[Comment]
	,[Content]
	,[Description])
select 
	'root' as [Wiki]
	,C.[Page]
	,P.[CreationDateTime]
	,C.[Namespace]
	,C.[Revision]
	,C.[Title]
	,C.[User]
	,C.[LastModified]
	,C.[Comment]
	,C.[Content]
	,C.[Description]
from [dbo].[PageContent_v3] C INNER JOIN [dbo].[Page_v3] P on C.[Namespace] = P.[Namespace]
	and C.[Page] = P.[Name]

insert into [dbo].[CategoryBinding]
	([Wiki]
	,[Namespace]
	,[Category]
	,[Page])
select 
	'root' as [Wiki]
	,[Namespace]
	,[Category]
	,[Page]
from [dbo].[CategoryBinding_v3]

insert into [dbo].[PageKeyword]
	([Wiki]
	,[Page]
	,[Namespace]
	,[Revision]
	,[Keyword])
select 
	'root' as [Wiki]
	,[Page]
	,[Namespace]
	,[Revision]
	,[Keyword]
from [dbo].[PageKeyword_v3]

insert into [dbo].[Message]
	([Wiki]
	,[Page]
	,[Namespace]
	,[Id]
	,[Parent]
	,[Username]
	,[Subject]
	,[DateTime]
	,[Body])
select 
	'root' as [Wiki]
	,[Page]
	,[Namespace]
	,[Id]
	,[Parent]
	,[Username]
	,[Subject]
	,[DateTime]
	,[Body]
from [dbo].[Message_v3]

insert into [dbo].[NavigationPath]
	([Wiki]
	,[Name]
	,[Namespace]
	,[Page]
	,[Number])
select 
	'root' as [Wiki]
	,[Name]
	,[Namespace]
	,[Page]
	,[Number]
from [dbo].[NavigationPath_v3]

insert into [dbo].[Snippet]
	([Wiki]
	,[Name]
	,[Content])
select 
	'root' as [Wiki]
	,[Name]
	,[Content]
from [dbo].[Snippet_v3]

insert into [dbo].[ContentTemplate]
	([Wiki]
	,[Name]
	,[Content])
select 
	'root' as [Wiki]
	,[Name]
	,[Content]
from [dbo].[ContentTemplate_v3]


update [Version] set [Version] = 4000 where [Component] = 'Pages'
