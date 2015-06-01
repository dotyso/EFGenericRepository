using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace CCWOnline.Management.EntityFramework
{
    public static class DbContextExtensions
    {
        public static void Delete<TEntity>(this DbSet<TEntity> dbSet, DbContext context, Expression<Func<TEntity, bool>> where) where TEntity : class
        {
            IQueryable<TEntity> clause = dbSet.Where<TEntity>(where);

            string snippet = "FROM [dbo].[";

            string sql = clause.ToString();
            string sqlFirstPart = sql.Substring(sql.IndexOf(snippet));

            sqlFirstPart = sqlFirstPart.Replace("AS [Extent1]", "");
            sqlFirstPart = sqlFirstPart.Replace("[Extent1].", "");


            context.Database.ExecuteSqlCommand(String.Format("DELETE {0}", sqlFirstPart));

        }
    }
}
