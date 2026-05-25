-- Run once on POS_DATABASE if LastActivityUtc is missing (Remember Me session tracking).
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.UserLog') AND name = N'LastActivityUtc'
)
BEGIN
    ALTER TABLE dbo.UserLog
        ADD LastActivityUtc DATETIMEOFFSET(2) NULL;
END
GO
