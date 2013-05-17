if not exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Namespace')
create table [Namespace] (
	[Wiki] varchar(100) not null,
	[Name] nvarchar(100) not null,
	[DefaultPage] nvarchar(200),
	constraint [PK_Namespace] primary key clustered ([Wiki], [Name])
)

if not exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Category')
create table [Category](
	[Wiki] varchar(100) not null,
	[Name] nvarchar(100) not null,
	[Namespace] nvarchar(100) not null,
	constraint [FK_Category_Namespace] foreign key ([Wiki], [Namespace]) references [Namespace]([Wiki], [Name])
		on delete cascade on update cascade,
	constraint [PK_Category] primary key clustered ([Wiki], [Name], [Namespace])
)

if not exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'PageContent')
create table [PageContent] (
	[Wiki] varchar(100) not null,
	[Name] nvarchar(200) not null,
	[CreationDateTime] datetime not null,
	[Namespace] nvarchar(100) not null,
	[Revision] smallint not null,
	[Title] nvarchar(200) not null,
	[User] nvarchar(100) not null,
	[LastModified] datetime not null,
	[Comment] nvarchar(300),
	[Content] nvarchar(max) not null,
	[Description] nvarchar(200),
	constraint [FK_Page_Namespace] foreign key ([Wiki], [Namespace]) references [Namespace]([Wiki], [Name])
		on delete cascade on update cascade,
	constraint [PK_PageContent] primary key clustered ([Wiki], [Name], [Namespace], [Revision])
)

if not exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'CategoryBinding')
-- Deleting/Renaming/Moving a page requires manually updating the binding
create table [CategoryBinding] (
	[Wiki] varchar(100) not null,
	[Namespace] nvarchar(100) not null,
	[Category] nvarchar(100) not null,
	[Page] nvarchar(200) not null,
		constraint [FK_CategoryBinding_Namespace] foreign key ([Wiki], [Namespace]) references [Namespace]([Wiki], [Name]),
	constraint [FK_CategoryBinding_Category] foreign key ([Wiki], [Category], [Namespace]) references [Category]([Wiki], [Name], [Namespace])
		on delete cascade on update cascade,
	constraint [PK_CategoryBinding] primary key clustered ([Wiki], [Namespace], [Page], [Category])
)

if not exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'PageKeyword')
create table [PageKeyword] (
	[Wiki] varchar(100) not null,
	[Page] nvarchar(200) not null,
	[Namespace] nvarchar(100) not null,
	[Revision] smallint not null,
	[Keyword] nvarchar(50) not null,
	constraint [FK_PageKeyword_PageContent] foreign key ([Wiki], [Page], [Namespace], [Revision]) references [PageContent]([Wiki], [Name], [Namespace], [Revision])
		on delete cascade on update cascade,
	constraint [PK_PageKeyword] primary key clustered ([Wiki], [Page], [Namespace], [Revision], [Keyword])
)

if not exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Message')
create table [Message] (
	[Wiki] varchar(100) not null,
	[Page] nvarchar(200) not null,
	[Namespace] nvarchar(100) not null,
	[Id] smallint not null,
	[Parent] smallint,
	[Username] nvarchar(100) not null,
	[Subject] nvarchar(200) not null,
	[DateTime] datetime not null,
	[Body] nvarchar(max) not null,
	constraint [PK_Message] primary key clustered ([Wiki], [Page], [Namespace], [Id])
)

if not exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'NavigationPath')
create table [NavigationPath] (
	[Wiki] varchar(100) not null,
	[Name] nvarchar(100) not null,
	[Namespace] nvarchar(100) not null,
	[Page] nvarchar(200) not null,
	[Number] smallint not null,
	constraint [PK_NavigationPath] primary key clustered ([Wiki], [Name], [Namespace], [Page])
)

if not exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Snippet')
create table [Snippet] (
	[Wiki] varchar(100) not null,
	[Name] nvarchar(200) not null,
	[Content] nvarchar(max) not null,
	constraint [PK_Snippet] primary key clustered ([Wiki], [Name])
)

if not exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'ContentTemplate')
create table [ContentTemplate] (
	[Wiki] varchar(100) not null,
	[Name] nvarchar(200) not null,
	[Content] nvarchar(max) not null,
	constraint [PK_ContentTemplate] primary key clustered ([Wiki], [Name])
)

if (select count(*) from sys.tables where [Name] = 'Version') = 0
begin
	create table [Version] (
		[Component] varchar(100) not null,
		[Version] int not null,
		constraint [PK_Version] primary key clustered ([Component])
	)
end

if (select count([Version]) from [Version] where [Component] = 'Pages') = 0
begin
	insert into [Version] ([Component], [Version]) values ('Pages', 4000)
end
