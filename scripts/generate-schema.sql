-- ============================================
-- Generate CREATE TABLE script for all tables
-- Run this against the source database
-- ============================================
SET NOCOUNT ON;

DECLARE @sql nvarchar(max) = '';

SELECT @sql = @sql + 
    'IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[' + t.name + ']'') AND type = ''U'')' + CHAR(13) +
    'BEGIN' + CHAR(13) +
    '    PRINT ''Creating table: ' + t.name + ''';' + CHAR(13) +
    'END' + CHAR(13) + 'GO' + CHAR(13) + CHAR(13)
FROM sys.tables t
WHERE t.is_ms_shipped = 0
ORDER BY t.name;

PRINT '-- Tables to check:';
PRINT @sql;
