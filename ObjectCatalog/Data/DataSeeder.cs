using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ObjectCatalog.Models;
using System.Data;

namespace ObjectCatalog.Data;

public static class DataSeeder
{
    public static async Task SeedDataV1(ObjectCatalogDbContext context)
    {
        Console.WriteLine("Starting database seeding...");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // 1. Очистка всех таблиц в правильном порядке (с учетом foreign key constraints)
        Console.WriteLine("Clearing existing data...");
        await ClearAllTables(context);
        Console.WriteLine($"Database cleared in {sw.ElapsedMilliseconds}ms");

        // 2. Seed Categories (50 categories)
        Console.WriteLine("Seeding Categories...");
        var categories = Enumerable.Range(1, 50)
            .Select(i => new Category { Name = $"Category {i}" })
            .ToList();

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
        Console.WriteLine($"Seeded {categories.Count} categories in {sw.ElapsedMilliseconds}ms");

        // 3. Seed Objects (1,000,000 objects)
        Console.WriteLine("Seeding Objects...");
        var objectBatch = new List<ObjectEntity>();
        var random = new Random();
        const int objectBatchSize = 5000;

        for (int i = 1; i <= 1_000_000; i++)
        {
            objectBatch.Add(new ObjectEntity
            {
                Name = $"Object {i}",
                Description = $"Description for Object {i}",
                Price = (decimal)(random.NextDouble() * 1000),
                CreatedDate = DateTime.UtcNow.AddDays(-random.Next(1, 365))
            });

            if (i % objectBatchSize == 0 || i == 1_000_000)
            {
                await context.Objects.AddRangeAsync(objectBatch);
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
                objectBatch.Clear();

                if (i % 100_000 == 0)
                    Console.WriteLine($"Seeded {i} objects...");
            }
        }
        Console.WriteLine($"Seeded 1,000,000 objects in {sw.ElapsedMilliseconds}ms");

        // 4. Seed ObjectCategories
        Console.WriteLine("Seeding Object Categories...");

        var objectIds = await context.Objects.AsNoTracking().Select(o => o.Id).ToListAsync();
        var categoryIds = await context.Categories.AsNoTracking().Select(c => c.Id).ToListAsync();

        await using var connection = new SqlConnection(context.Database.GetConnectionString());
        await connection.OpenAsync();

        // Временное отключение индексов для ускорения вставки
        await DisableIndexes(connection);

        var dataTable = new DataTable();
        dataTable.Columns.Add("ObjectId", typeof(int));
        dataTable.Columns.Add("CategoryId", typeof(int));

        using var bulkCopy = new SqlBulkCopy(connection)
        {
            DestinationTableName = "ObjectCategories",
            BatchSize = 10_000,
            BulkCopyTimeout = 3600
        };

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
                    dataTable.Rows.Add(objectId, categoryId);
                    relationsCount++;

                    if (dataTable.Rows.Count >= 50_000)
                    {
                        await bulkCopy.WriteToServerAsync(dataTable);
                        dataTable.Clear();
                        Console.WriteLine($"Seeded {relationsCount} relations...");
                    }
                }
            }
        }

        if (dataTable.Rows.Count > 0)
            await bulkCopy.WriteToServerAsync(dataTable);

        // Восстановление индексов
        await RebuildIndexes(connection);

        Console.WriteLine($"Seeded {relationsCount} object-category relations in {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"Database seeding completed in {sw.Elapsed.TotalMinutes:0.00} minutes");
    }

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

    private static async Task DisableIndexes(SqlConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            DECLARE @sql NVARCHAR(MAX) = N'';
            
            SELECT @sql = @sql + 
                'ALTER INDEX ' + i.name + ' ON ' + t.name + ' DISABLE; '
            FROM sys.indexes i
            JOIN sys.tables t ON i.object_id = t.object_id
            WHERE i.type_desc = 'NONCLUSTERED' 
            AND t.name IN ('ObjectCategories');
            
            EXEC sp_executesql @sql;";

        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task RebuildIndexes(SqlConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            DECLARE @sql NVARCHAR(MAX) = N'';
            
            SELECT @sql = @sql + 
                'ALTER INDEX ' + i.name + ' ON ' + t.name + ' REBUILD; '
            FROM sys.indexes i
            JOIN sys.tables t ON i.object_id = t.object_id
            WHERE i.type_desc = 'NONCLUSTERED' 
            AND t.name IN ('ObjectCategories');
            
            EXEC sp_executesql @sql;";

        await cmd.ExecuteNonQueryAsync();
    }
}