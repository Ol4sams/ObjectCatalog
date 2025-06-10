using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ObjectCatalog.Models;
using System.Data;

namespace ObjectCatalog.Data;

public class DataSeeder
{
    public static async Task SeedDataV2(ObjectCatalogDbContext context)
    {
        Console.WriteLine("Creating DB...");
        await context.Database.EnsureCreatedAsync();

        Console.WriteLine("Starting database seeding...");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        Console.WriteLine("Clearing existing data...");
        await ClearAllTables(context);
        Console.WriteLine($"Database cleared in {sw.ElapsedMilliseconds}ms");

        context.ChangeTracker.AutoDetectChangesEnabled = false;
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        Console.WriteLine("Seeding Categories...");
        await SeedCategories(context);
        Console.WriteLine($"Seeded categories in {sw.ElapsedMilliseconds}ms");

        Console.WriteLine("Seeding Objects...");
        await SeedObjects(context);
        Console.WriteLine($"Seeded 1,000,000 objects in {sw.ElapsedMilliseconds}ms");

        Console.WriteLine("Seeding Object Categories...");
        await SeedObjectCategories(context);
        Console.WriteLine($"Seeded object-category relations in {sw.ElapsedMilliseconds}ms");

        context.ChangeTracker.AutoDetectChangesEnabled = true;
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

        Console.WriteLine($"Database seeding completed in {sw.Elapsed.TotalMinutes:0.00} minutes");
    }

    private static async Task SeedCategories(ObjectCatalogDbContext context)
    {
        var categories = Enumerable.Range(1, 50)
            .Select(i => new Category { Name = $"Category {i}" })
            .ToList();

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }

    private static async Task SeedObjects(ObjectCatalogDbContext context)
    {
        await using var connection = new SqlConnection(context.Database.GetConnectionString());
        await connection.OpenAsync();

        // Создаем DataTable для объектов
        var objectTable = new DataTable();
        objectTable.Columns.Add("Name", typeof(string));
        objectTable.Columns.Add("Description", typeof(string));
        objectTable.Columns.Add("Price", typeof(decimal));
        objectTable.Columns.Add("CreatedDate", typeof(DateTime));

        var random = new Random();
        const int batchSize = 100_000;

        using var bulkCopy = new SqlBulkCopy(connection,
            SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.UseInternalTransaction,
            null)
        {
            DestinationTableName = "Objects",
            BatchSize = batchSize,
            BulkCopyTimeout = 0
        };

        bulkCopy.ColumnMappings.Add("Name", "Name");
        bulkCopy.ColumnMappings.Add("Description", "Description");
        bulkCopy.ColumnMappings.Add("Price", "Price");
        bulkCopy.ColumnMappings.Add("CreatedDate", "CreatedDate");

        for (int i = 1; i <= 1_000_000; i++)
        {
            objectTable.Rows.Add(
                $"Object {i}",
                $"Description for Object {i}",
                (decimal)(random.NextDouble() * 1000),
                DateTime.UtcNow.AddDays(-random.Next(1, 365))
            );

            if (i % batchSize == 0 || i == 1_000_000)
            {
                await bulkCopy.WriteToServerAsync(objectTable);
                objectTable.Clear();
                Console.WriteLine($"Seeded {i} objects...");
            }
        }
    }

    private static async Task SeedObjectCategories(ObjectCatalogDbContext context)
    {
        await using var connection = new SqlConnection(context.Database.GetConnectionString());
        await connection.OpenAsync();
        
        var objectIds = await context.Objects.AsNoTracking().Select(o => o.Id).ToListAsync();
        var categoryIds = await context.Categories.AsNoTracking().Select(c => c.Id).ToListAsync();

        var relationTable = new DataTable();
        relationTable.Columns.Add("ObjectId", typeof(int));
        relationTable.Columns.Add("CategoryId", typeof(int));

        using var bulkCopy = new SqlBulkCopy(connection,
            SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.UseInternalTransaction,
            null)
        {
            DestinationTableName = "ObjectCategories",
            BatchSize = 50_000,
            BulkCopyTimeout = 0
        };

        var random = new Random();
        long relationsCount = 0;

        foreach (var objectId in objectIds)
        {
            if (random.NextDouble() < 0.9)
            {
                int categoryCount = random.Next(1, 4);
                var selectedCategories = categoryIds
                    .OrderBy(_ => random.Next())
                    .Take(categoryCount);

                foreach (var categoryId in selectedCategories)
                {
                    relationTable.Rows.Add(objectId, categoryId);
                    relationsCount++;

                    if (relationTable.Rows.Count >= 50_000)
                    {
                        await bulkCopy.WriteToServerAsync(relationTable);
                        relationTable.Clear();
                        Console.WriteLine($"Seeded {relationsCount} relations...");
                    }
                }
            }
        }

        if (relationTable.Rows.Count > 0)
        {
            await bulkCopy.WriteToServerAsync(relationTable);
        }
    }



    //public static async Task SeedDataV1(ObjectCatalogDbContext context)
    //{
    //    Console.WriteLine("Creating DB...");
    //    await context.Database.EnsureCreatedAsync();;

    //    Console.WriteLine("Starting database seeding...");
    //    var sw = System.Diagnostics.Stopwatch.StartNew();

    //    Console.WriteLine("Clearing existing data...");
    //    await ClearAllTables(context);
    //    Console.WriteLine($"Database cleared in {sw.ElapsedMilliseconds}ms");

    //    Console.WriteLine("Seeding Categories...");
    //    var categories = Enumerable.Range(1, 50)
    //        .Select(i => new Category { Name = $"Category {i}" })
    //        .ToList();

    //    await context.Categories.AddRangeAsync(categories);
    //    await context.SaveChangesAsync();
    //    Console.WriteLine($"Seeded {categories.Count} categories in {sw.ElapsedMilliseconds}ms");

    //    Console.WriteLine("Seeding Objects...");
    //    var objectBatch = new List<ObjectEntity>();
    //    var random = new Random();
    //    const int objectBatchSize = 5000;

    //    for (int i = 1; i <= 1_000_000; i++)
    //    {
    //        objectBatch.Add(new ObjectEntity
    //        {
    //            Name = $"Object {i}",
    //            Description = $"Description for Object {i}",
    //            Price = (decimal)(random.NextDouble() * 1000),
    //            CreatedDate = DateTime.UtcNow.AddDays(-random.Next(1, 365))
    //        });

    //        if (i % objectBatchSize == 0 || i == 1_000_000)
    //        {
    //            await context.Objects.AddRangeAsync(objectBatch);
    //            await context.SaveChangesAsync();
    //            context.ChangeTracker.Clear();
    //            objectBatch.Clear();

    //            if (i % 100_000 == 0)
    //                Console.WriteLine($"Seeded {i} objects...");
    //        }
    //    }
    //    Console.WriteLine($"Seeded 1,000,000 objects in {sw.ElapsedMilliseconds}ms");

    //    Console.WriteLine("Seeding Object Categories...");

    //    var objectIds = await context.Objects.AsNoTracking().Select(o => o.Id).ToListAsync();
    //    var categoryIds = await context.Categories.AsNoTracking().Select(c => c.Id).ToListAsync();

    //    await using var connection = new SqlConnection(context.Database.GetConnectionString());
    //    await connection.OpenAsync();        

    //    var dataTable = new DataTable();
    //    dataTable.Columns.Add("ObjectId", typeof(int));
    //    dataTable.Columns.Add("CategoryId", typeof(int));

    //    using var bulkCopy = new SqlBulkCopy(connection)
    //    {
    //        DestinationTableName = "ObjectCategories",
    //        BatchSize = 10_000,
    //        BulkCopyTimeout = 3600
    //    };

    //    long relationsCount = 0;
    //    foreach (var objectId in objectIds)
    //    {
    //        if (random.NextDouble() < 0.9)
    //        {
    //            int categoryCount = random.Next(1, 4);
    //            var selectedCategories = categoryIds
    //                .OrderBy(_ => random.Next())
    //                .Take(categoryCount);

    //            foreach (var categoryId in selectedCategories)
    //            {
    //                dataTable.Rows.Add(objectId, categoryId);
    //                relationsCount++;

    //                if (dataTable.Rows.Count >= 50_000)
    //                {
    //                    await bulkCopy.WriteToServerAsync(dataTable);
    //                    dataTable.Clear();
    //                    Console.WriteLine($"Seeded {relationsCount} relations...");
    //                }
    //            }
    //        }
    //    }

    //    if (dataTable.Rows.Count > 0)
    //        await bulkCopy.WriteToServerAsync(dataTable);        

    //    Console.WriteLine($"Seeded {relationsCount} object-category relations in {sw.ElapsedMilliseconds}ms");
    //    Console.WriteLine($"Database seeding completed in {sw.Elapsed.TotalMinutes:0.00} minutes");
    //}



    private static async Task ClearAllTables(ObjectCatalogDbContext context)
    {
        // Отключаем проверку ограничений для быстрой очистки
        await context.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");

        // Очищаем таблицы в правильном порядке (сначала зависимые)
        await context.Database.ExecuteSqlRawAsync("DELETE FROM ObjectCategories");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Objects");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Categories");

        // Включаем проверку ограничений обратно
        await context.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'");

        // Сбрасываем идентификаторы
        await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Categories', RESEED, 0)");
        await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Objects', RESEED, 0)");
    }    
}