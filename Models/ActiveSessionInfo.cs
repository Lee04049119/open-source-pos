using System;

namespace Models
{
    /// <summary>Describes another active remembered session (for login confirmation).</summary>
    public class ActiveSessionInfo
    {
        public string Browser { get; set; }
        public string Os { get; set; }
        public string Device { get; set; }
        public string City { get; set; }
        public string Country_name { get; set; }
        public DateTimeOffset? SessStart { get; set; }
        public DateTimeOffset? LastActivityUtc { get; set; }
    }
}
