using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ef_bug
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });

            using (var ctx = CreateContext(loggerFactory))
            {
                await ctx.Database.EnsureDeletedAsync();
                await ctx.Database.EnsureCreatedAsync();

                await CreateDemoData(ctx);

                /*
                 * The following throws an exception every time the parameter 'items' matches the enum value in the MapToViewModel expression below
                 * EF tries to insert the enum string value into the SELECT TOP(xxx) expression which results in
                 * 'Microsoft.Data.SqlClient.SqlException (0x80131904): The number of rows provided for a TOP or FETCH clauses row count parameter must be an integer.'
                 */
                var result = await CreatePagedResult(ctx.Orders.Where(x => x.Items.Any()), ctx.Orders, x => x.Id,
                    MapToViewModelExpression, items: 1, skippedItems: 0);

                Console.WriteLine(result.Count);
            }
        }

        private static Expression<Func<Order, ProjectedOrder>> MapToViewModelExpression =>
            entity => new ProjectedOrder
            {
                Id = entity.Id,
                SpecialSum = entity.Items.Where(x => x.Type == OrderItemType.MyType1)
                    .Select(x => x.Price)
                    .FirstOrDefault()

            };

        private static DemoContext CreateContext(ILoggerFactory loggerFactory)
        {
  
                var options = new DbContextOptionsBuilder<DemoContext>()
                    .UseSqlServer(
                        @"Server=(localdb)\mssqllocaldb;Database=ef_bug;Trusted_Connection=False;MultipleActiveResultSets=true")
                    .EnableDetailedErrors()
                    .UseLoggerFactory(loggerFactory)
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                    .EnableSensitiveDataLogging();

                return new DemoContext(options.Options);

        }

        private static async Task<List<TProjection>> CreatePagedResult<T, TProjection, TKey>(
            IQueryable<T> preFilteredQuery,
            IQueryable<T> unfilteredQuery,
            Expression<Func<T, TKey>> keySelector,
            Expression<Func<T, TProjection>> projection,
            int items,
            int skippedItems
        )
        {
            var localQuery = preFilteredQuery;
            localQuery = localQuery.OrderBy(keySelector).Take(items);
            if (skippedItems > 0) localQuery = localQuery.Skip(skippedItems);

            var keys = localQuery.Select(keySelector);
            var result = keys.Join(unfilteredQuery,
                    x => x,
                    keySelector,
                    (x, y) => y
                )
                .Select(projection);

            return await result.ToListAsync();
        }

        private static async Task CreateDemoData(DemoContext ctx)
        {
            var orders = FizzWare.NBuilder.Builder<Order>.CreateListOfSize(50)
                .All()
                .With(x => x.Items = Builder<OrderItem>.CreateListOfSize(10)
                    .All()
                    .With(item => item.Id = Guid.NewGuid())
                    .With(item => item.OrderId = x.Id)
                    .With(item => item.Type = OrderItemType.MyType2)
                    .Random(1)
                    .With(item => item.Type = OrderItemType.MyType1)
                    .Build())
                .Build();

            await ctx.Orders.AddRangeAsync(orders);

            await ctx.SaveChangesAsync();
        }
    }
}
