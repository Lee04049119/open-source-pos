CREATE PROCEDURE [dbo].[usp_UserSession_GetActiveRemembered]
    @UserID BIGINT,
    @InactivityDays INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        UserLogID, UserID, RememberUser, SessionToken, SessStart, SessEnd, TokenExpirationDate,
        LastActivityUtc, browser, os, device, City, Country_name
    FROM dbo.UserLog
    WHERE UserID = @UserID
      AND RememberUser = 1
      AND IsLoginSuccessful = 1
      AND SessEnd IS NULL
      AND TokenExpirationDate > SYSDATETIMEOFFSET()
      AND COALESCE(LastActivityUtc, SessStart) > DATEADD(DAY, -@InactivityDays, SYSDATETIMEOFFSET())
    ORDER BY COALESCE(LastActivityUtc, SessStart) DESC;
END
GO
