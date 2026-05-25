CREATE PROCEDURE [dbo].[usp_UserSession_UpdateLastActivity]
    @UserID BIGINT,
    @SessionToken VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.UserLog
    SET LastActivityUtc = SYSDATETIMEOFFSET()
    WHERE UserID = @UserID
      AND SessionToken = @SessionToken
      AND RememberUser = 1
      AND IsLoginSuccessful = 1
      AND SessEnd IS NULL;
END
GO
