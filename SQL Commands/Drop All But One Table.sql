DECLARE @Table TABLE(name TEXT, rowNumber INT)

DECLARE @buildCounter int = 0
DECLARE @MaxBuildCounter int = (SELECT COUNT(*) FROM sysobjects WHERE xtype='U')
WHILE (@buildCounter <= @MaxBuildCounter)
BEGIN
	IF @buildCounter > @MaxBuildCounter
	BEGIN
		BREAK
	END
	INSERT INTO @Table VALUES((SELECT name FROM sysobjects WHERE xtype='U' ORDER BY name OFFSET @buildCounter ROWS FETCH NEXT 1 ROWS ONLY), @buildCounter + 1)
	SET @buildCounter += 1
END

DECLARE @Counter int = 0
DECLARE @MaxCounter int = (SELECT COUNT(*) FROM sysobjects WHERE xtype='U')
DECLARE @tableName VARCHAR(100)
WHILE (@Counter <= @MaxCounter)
BEGIN
	SET @tableName = (SELECT name FROM @Table WHERE rowNumber = (@Counter + 1))
	PRINT 'Checking table : ' + CAST(@Counter AS CHAR(2)) + ' of: ' + CAST(@MaxCounter AS CHAR(2))
	IF @Counter = @MaxCounter
		BEGIN
		PRINT'About to truncate now...'
		EXECUTE('TRUNCATE TABLE Subjects_Master')
		EXECUTE('TRUNCATE TABLE Sentences_Master')
		EXECUTE('TRUNCATE TABLE Articles_Master')
		BREAK
		END
	IF @tableName <> 'Subjects_Master' AND @tableName <> 'Sentences_Master' AND @tableName <> 'Articles_Master'
	BEGIN
		PRINT 'I will drop the table: ' + @tableName
		PRINT 'Dropping table: ' + CAST(@Counter AS CHAR(2)) + ' of: ' + CAST(@MaxCounter AS CHAR(2))
		EXECUTE ('DROP TABLE [' + @tableName + ']')
	END
	SET @Counter += 1
END

