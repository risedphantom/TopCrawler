create procedure [dbo].[ObjectGetBySysName]
	@TypeName varchar(200),
	@ObjectSysName varchar(200),
	@Result bigint out
as
begin
	declare	@Cmd nvarchar(max)

	
	if object_id(@TypeName, N'U') is null
	begin
		set @Result = -1
		return
	end

	if @ObjectSysName is null
	begin
		set @Result = null
		return
	end

	set @Cmd = N'select @Res = ID from ' + @TypeName + ' where SysName = ''' + @ObjectSysName + ''''
	
	exec sp_executesql @Cmd, N'@Res bigint out', @Res = @Result out
end
