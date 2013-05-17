/*
	GlobalSettings is a new component on this version, but the following tables were moved from Settings:
	- Log: a new column "Wiki" was added.
	- PluginAssembly: the columns are the same but the values are not migrated on this script to avoid errors with the assemblies from previous versions.
*/

if not exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'GlobalSetting')
create table [GlobalSetting] (
	[Name] varchar(100) not null,
	[Value] nvarchar(4000) not null,
	constraint [PK_GlobalSetting] primary key clustered ([Name])
)

if not exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Log') 
	begin
		create table [Log] (
			[Id] int not null identity,
			[DateTime] datetime not null,
			[EntryType] char not null,
			[User] nvarchar(100) not null,
			[Message] nvarchar(4000) not null,
			[Wiki] nvarchar(100),
			constraint [PK_Log] primary key clustered ([Id])
		)
	end
else if not exists(select * from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = 'Log' and COLUMN_NAME = 'Wiki') 
	begin
		-- Migrate from v3
		exec sp_rename 'PK_Log', 'PK_Log_v3';
		exec sp_rename 'Log', 'Log_v3';
		exec sp_rename 'PK_PluginAssembly', 'PK_PluginAssembly_v3'
		exec sp_rename 'PluginAssembly', 'PluginAssembly_v3'

		create table [Log] (
			[Id] int not null identity,
			[DateTime] datetime not null,
			[EntryType] char not null,
			[User] nvarchar(100) not null,
			[Message] nvarchar(4000) not null,
			[Wiki] nvarchar(100),
			constraint [PK_Log] primary key clustered ([Id])
		)

		insert into [dbo].[Log]
			([DateTime]
			,[EntryType]
			,[User]
			,[Message]
			,[Wiki])
		select 
			 [DateTime]
			,[EntryType]
			,[User]
			,[Message]
			,'root' as [Wiki]
		from [dbo].[Log_v3]
	end
	
if not exists(select * from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = 'PluginAssembly')
begin
	create table [PluginAssembly] (
		[Name] varchar(100) not null,
		[Assembly] varbinary(max) not null,
		constraint [PK_PluginAssembly] primary key clustered ([Name])
	)
end

if (select count(*) from sys.tables where [Name] = 'Version') = 0
begin
	create table [Version] (
		[Component] varchar(100) not null,
		[Version] int not null,
		constraint [PK_Version] primary key clustered ([Component])
	)
end

if (select count([Version]) from [Version] where [Component] = 'GlobalSettings') = 0
begin
	insert into [Version] ([Component], [Version]) values ('GlobalSettings', 4000)
end