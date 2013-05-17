-- Script to drop v3 tables from the database

declare @table_schema nvarchar(128)
declare @table_name nvarchar(128)

while exists (select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME like '%_v3')
begin
	select @table_schema = TABLE_SCHEMA, @table_name = TABLE_NAME 
	from INFORMATION_SCHEMA.TABLES
	where TABLE_NAME like '%_v3'

	-- Drop referencing keys
	declare @sql nvarchar(MAX)
	while exists (
		select *
		from
			sys.foreign_keys AS f
		inner join
			sys.foreign_key_columns AS fc
			on f.OBJECT_ID = fc.constraint_object_id
		where
			OBJECT_NAME (f.referenced_object_id) = @table_name
	)
	begin
		select @sql =
			'ALTER TABLE ' +
			OBJECT_NAME (f.parent_object_id) +
			' DROP CONSTRAINT ' +
			f.name +
			''
		from
			sys.foreign_keys AS f inner join
			sys.foreign_key_columns AS fc on f.OBJECT_ID = fc.constraint_object_id
		where
			OBJECT_NAME (f.referenced_object_id) = @table_name

		print @sql
		exec sp_executesql @sql
	end

	select @sql = N'drop table ' + @table_schema + '.' + @table_name
	print @sql
	exec sp_executesql @sql
end
