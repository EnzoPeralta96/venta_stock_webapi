using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace venta_stock_webapi.Shared.Paged
{
    public class PagedList<T>
    {
        public List<T> Items { get; private set; }
        public int PagedIndex { get; private set; }
        public int PageSize { get; private set; }
        public int TotalPages { get; private set; }
        public int TotalCount { get; private set; }
        public bool HasPrevioPage { get; private set; }
        public bool HasNextPage { get; private set; }

        public PagedList(List<T> items, int count, int pagedIndex, int pageSize)
        {
            Items = items;
            TotalCount = count;
            PagedIndex = pagedIndex;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize); //100 / 10 -> 10 paginas
            HasPrevioPage = PagedIndex > 1; 
            HasNextPage = PagedIndex < TotalPages;
        }

            
        public static async Task<PagedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedList<T>(items, count, pageIndex, pageSize);
        }   
    }
}