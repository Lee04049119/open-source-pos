using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Models;
using Repositories.SqlServer;
using Repositories.Log;
using Dapper;
using System.Data;
using System.Linq;

namespace Repositories
{
    public class ItemRepository : IItemRepository
    {
        private readonly IRepository _repo;
        private readonly ILogIt _log;

        public ItemRepository(IRepository repo, ILogIt logIt)
        {
            _repo = repo;
            _log = logIt;
        }
        
        public async Task<int> AddDataAsync(PosItem posItem)
        {
            try
            {
                return await _repo.WithConnection(async cmd =>
                {
                    string sql = @"
                    DECLARE @MMAX_ID INT                                      

                    SET @MMAX_ID = ISNULL( (SELECT max( isnull( ItemId, 0)) FROM PosItem) ,0)
                        
                    SET @MMAX_ID = @MMAX_ID + 1                                                          

                    INSERT INTO PosItem(ItemId, CustomCode, Description, ShortDesc, SalePrice, CreateUser, CreateDate, CompanyID)
                    VALUES(@MMAX_ID, @CustomCode, @Description, @ShortDesc, @SalePrice, @CreateUser, GETDATE(), @CompanyID)
                    SELECT @MMAX_ID
                        ; ";

                    var res = await cmd.QueryAsync<int>(sql, posItem);
                    var result = res.FirstOrDefault();
                    return result;

                });

            }
            catch (Exception ex)
            {
                _log.ExceptionLogFunc(ex);
                return Task.FromException<int>(ex).Result;
            }
        }

        public async Task<List<PosItem>> GetItemsAsync(string query, int companyId, int limit, int offset)
        {
            try
            {
                return await _repo.WithConnection(async c =>
                {
                    string sqlSearchItems = @"SELECT TOP 500
                        CAST(ItemId AS VARCHAR(20)) AS ItemId,
                        CustomCode,
                        Description,
                        ShortDesc,
                        ISNULL(SalePrice, 0) AS SalePrice,
                        @COMPANY_ID AS CompanyID
                        FROM PosItem
                        WHERE (ISNULL(CustomCode, '') + ISNULL(Description, '')) LIKE '%' + @QUERY + '%'
                        AND RTRIM(CAST(CompanyID AS VARCHAR(20))) = RTRIM(CAST(@COMPANY_ID AS VARCHAR(20)))
                        ORDER BY Description; ";

                    var searchItems = await c.QueryAsync<PosItem>(sqlSearchItems, new { QUERY = query ?? "", COMPANY_ID = companyId });
                    return searchItems.ToList();
                });
            }
            catch (Exception ex)
            {
                _log.ExceptionLogFunc(ex);
                throw;
            }
        }

        public async Task<int> DeleteDataAsync(string itemId, int companyId)
        {
            try
            {
                return await _repo.WithConnection(async cmd =>
                {
                    const string inUseSql = @"
                        SELECT COUNT(1) FROM InvoiceDetailItems
                        WHERE RTRIM(CAST(ItemCode AS VARCHAR(50))) = RTRIM(@ItemId)
                        AND RTRIM(CAST(CompanyID AS VARCHAR(20))) = RTRIM(CAST(@CompanyID AS VARCHAR(20)));";

                    var inUse = await cmd.ExecuteScalarAsync<int>(inUseSql, new { ItemId = itemId, CompanyID = companyId });
                    if (inUse > 0)
                        return -1;

                    const string deleteSql = @"
                        DELETE FROM PosItem
                        WHERE RTRIM(CAST(ItemId AS VARCHAR(20))) = RTRIM(@ItemId)
                        AND RTRIM(CAST(CompanyID AS VARCHAR(20))) = RTRIM(CAST(@CompanyID AS VARCHAR(20)));
                        SELECT @@ROWCOUNT;";

                    return await cmd.ExecuteScalarAsync<int>(deleteSql, new { ItemId = itemId, CompanyID = companyId });
                });
            }
            catch (Exception ex)
            {
                _log.ExceptionLogFunc(ex);
                return Task.FromException<int>(ex).Result;
            }
        }

        public async Task<int> UpdDataAsync(PosItem posItem)
        {
            try
            {
                return await _repo.WithConnection(async cmd =>
                {
                    string sql = @"
                    UPDATE PosItem SET
                    CustomCode = @CustomCode, 
                    Description = @Description, 
                    ShortDesc = @ShortDesc,
                    SalePrice = @SalePrice,
                    UpdateUser = @UpdateUser,
                    UpdateDate = GETDATE()
                    WHERE ItemId = @ItemId AND CompanyID = @CompanyID
                    SELECT @@ROWCOUNT
                    ; ";

                    var res = await cmd.QueryAsync<int>(sql,posItem);
                    var result = res.FirstOrDefault();
                    return result;

                });

            }
            catch (Exception ex)
            {
                _log.ExceptionLogFunc(ex);
                return Task.FromException<int>(ex).Result;
            }

        }
    }
}
