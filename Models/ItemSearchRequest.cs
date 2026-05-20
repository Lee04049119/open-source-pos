namespace Models
{
    /// <summary>
    /// Body for POST /api/item/getitems (Angular sends camelCase property names).
    /// </summary>
    public class ItemSearchRequest
    {
        public string query { get; set; } = "";
        public int companyId { get; set; }
        public int limit { get; set; }
        public int offset { get; set; }
    }
}
