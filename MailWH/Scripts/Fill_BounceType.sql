if not exists (select top 1 1
				from BounceType
				where SysName = 'BounceTypeUnknown')
begin
	insert	BounceType(SysName, Name)
	values	('BounceTypeUnknown', 'Unknown')
end

if not exists (select top 1 1
				from BounceType
				where SysName = 'BounceTypeNotFound')
begin
	insert	BounceType(SysName, Name)
	values	('BounceTypeNotFound', 'Адрес не найден')
end

if not exists (select top 1 1
				from BounceType
				where SysName = 'BounceTypeFull')
begin
	insert	BounceType(SysName, Name)
	values	('BounceTypeFull', 'Почтовый ящик переполнен')
end

if not exists (select top 1 1
				from BounceType
				where SysName = 'BounceTypeOutOfOffice')
begin
	insert	BounceType(SysName, Name)
	values	('BounceTypeOutOfOffice', '"Out of office" автоответ')
end

if not exists (select top 1 1
				from BounceType
				where SysName = 'BounceTypeTimeout')
begin
	insert	BounceType(SysName, Name)
	values	('BounceTypeTimeout', 'Время доставки истекло')
end

if not exists (select top 1 1
				from BounceType
				where SysName = 'BounceTypeRefused')
begin
	insert	BounceType(SysName, Name)
	values	('BounceTypeRefused', 'Отказано в доставке')
end

if not exists (select top 1 1
				from BounceType
				where SysName = 'BounceTypeInactive')
begin
	insert	BounceType(SysName, Name)
	values	('BounceTypeInactive', 'Почтовый ящик неактивен')
end

if not exists (select top 1 1
				from BounceType
				where SysName = 'BounceTypeHostNotFound')
begin
	insert	BounceType(SysName, Name)
	values	('BounceTypeHostNotFound', 'Хост не найден')
end

if not exists (select top 1 1
				from BounceType
				where SysName = 'BounceTypeManyConnections')
begin
	insert	BounceType(SysName, Name)
	values	('BounceTypeManyConnections', 'Превышен лимит соединений с указанного IP')
end

if not exists (select top 1 1
				from BounceType
				where SysName = 'BounceTypeSpam')
begin
	insert	BounceType(SysName, Name)
	values	('BounceTypeSpam', 'Письмо не доставлено, так как является спамом')
end
GO