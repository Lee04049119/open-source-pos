namespace Models
{
    public class AppSettings
    {
        public string Secret { get; set; }
        /// <summary>JWT lifetime when Remember Me is not checked (hours).</summary>
        public string TokenExpiresInHours { get; set; }
        /// <summary>Access JWT lifetime when Remember Me is checked (minutes).</summary>
        public string AccessTokenExpiresMinutes { get; set; } = "15";
        /// <summary>Refresh/session lifetime when Remember Me is checked (days).</summary>
        public string RefreshTokenExpiresDays { get; set; } = "30";
        /// <summary>End remembered session after this many days without activity.</summary>
        public string InactivityExpiresDays { get; set; } = "30";
    }
}
