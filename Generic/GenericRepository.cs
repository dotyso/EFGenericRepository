using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Text;


namespace CCWOnline.Management.EntityFramework
{
    public abstract class GenericRepository<TContext, TEntity>
        where TEntity : class
        where TContext : DbContext, new()
    {
        private TContext _context = new TContext();

        public TContext Context
        {
            get { return _context; }
            set { _context = value; }
        }

        public virtual TEntity Create(TEntity entity)
        {
            _context.Set<TEntity>().Add(entity);
            _context.SaveChanges();
            return entity;
        }

        public virtual TEntity Update(TEntity entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
            return entity;
        }
        public virtual IQueryable<TEntity> FindAll()
        {
            IQueryable<TEntity> list = _context.Set<TEntity>();
            return list;
        }
        public virtual TEntity FindOne(Expression<Func<TEntity, bool>> whereClause)
        {
            TEntity entity = null;

            if (whereClause != null)
                entity = _context.Set<TEntity>().SingleOrDefault(whereClause);

            return entity;
        }

        public virtual int Count(Expression<Func<TEntity, bool>> whereClause = null)
        {
            if (whereClause == null)
                return _context.Set<TEntity>().Count();
            else
                return _context.Set<TEntity>().Where(whereClause).Count();
        }

        public virtual bool IsExist(Expression<Func<TEntity, bool>> whereClause)
        {
            if (_context.Set<TEntity>().Where(whereClause).Count() == 0)
                return false;
            else
                return true;
        }

        public virtual void Delete(TEntity entity)
        {
            _context.Set<TEntity>().Remove(entity);
            _context.SaveChanges();
        }
        public virtual void Delete(Expression<Func<TEntity, bool>> whereClause)
        {
            _context.Set<TEntity>().Delete(_context, whereClause);
        }

        public virtual List<TEntity> FindAll(Query<TEntity> query)
        {

            IOrderedQueryable<TEntity> orderedList = null;

            //构建where
            IQueryable<TEntity> list = null;
            if (query.WhereClause != null)
                list = _context.Set<TEntity>().Where(query.WhereClause);
            else if (!String.IsNullOrEmpty(query.WhereString))
                list = _context.Set<TEntity>().Where(query.WhereString);
            else
                list = _context.Set<TEntity>();

            if (query.OrderByClause != null)
            { 
                //构建orderby
                orderedList = BuildOrderedList(query, orderedList, list);

                if (orderedList != null)
                {
                    list = orderedList.AsQueryable();
                }

            }
            else if (!String.IsNullOrEmpty(query.OrderByString))
            {
                list = list.OrderBy(query.OrderByString);
            }


            if (query.Limit != null)
                list = (IQueryable<TEntity>)list.Take((int)query.Limit);

            return list.ToList();
        }

        public virtual List<TEntity> FindAll(Expression<Func<TEntity, bool>> whereClause, OrderByClause<TEntity> orderByClause = null)
        {

            Query<TEntity> query = new Query<TEntity>();
            query.WhereClause = whereClause;
            query.OrderByClause = orderByClause;

            return FindAll(query).ToList();
        }

        public virtual List<TEntity> FindAll(Query<TEntity> query, int pageIndex, int pageSize, out int totalCount)
        {
            totalCount = _context.Set<TEntity>().C