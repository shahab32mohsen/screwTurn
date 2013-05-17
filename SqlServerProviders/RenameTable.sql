/*
	Script to rename a table and all its constraints to a version.
	Does not attempt to rename if the new table name already exists in the database.
	Expects the following parameters to be defined:
		@table nvarchar(128) - Name of the table that will be renamed, example: @table='Page'
		@suffix nvarchar(10) - Suffix with version number that will append to the object name, example: @suffix='_v3'
*/
DECLARE @objects as TABLE (
	name nvarchar(128)
)

IF NOT EXISTS (select TABLE_NAME from INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @table + @suffix)
BEGIN
	-- list all constraints associated with the table, plus the table itself if the original name matches an existing table in the database
	insert into @objects
	select TABLE_NAME as name 
	from INFORMATION_SCHEMA.TABLES
	WHERE TABLE_NAME = @table
	UNION
	select CONSTRAINT_NAME as name
	from INFORMATION_SCHEMA.TABLE_CONSTRAINTS
	WHERE TABLE_NAME = @table

	-- select the next object to handle
	DECLARE @name nvarchar(128)
	select TOP 1 @name = name
	from @objects

	WHILE @name IS NOT NULL AND @@ROWCOUNT > 0
	BEGIN
		DECLARE @newname nvarchar(128)
		SET @newname = @name + @suffix
		exec sp_rename @name, @newname

		DELETE from @objects 
		WHERE name = @name

		select TOP 1 @name = name
		from @objects
	END
END