CREATE PROCEDURE [dbo].[usp_UserSession_EndOtherRemembered]
    @UserID BIGINT,
    @KeepSessionToken VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.UserLog
    SET SessEnd = SYSDATETIMEOFFSET(),
        RememberUser = 0
    WHERE UserID = @UserID
      AND RememberUser = 1
      AND IsLoginSuccessful = 1
      AND SessEnd IS NULL
      AND SessionToken <> @KeepSessionToken;
END
GO
