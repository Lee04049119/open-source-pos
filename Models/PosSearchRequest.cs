namespace Models
{
    /// <summary>
    /// Body for POST /api/POS/GetSearchItems (Angular sends Query and companyId).
    /// </summary>
    public class PosSearchRequest
    {
        public string Query { get; set; } = "";
        public int companyId { get; set; }

        public string GetSearchText() => Query ?? "";
    }
}
