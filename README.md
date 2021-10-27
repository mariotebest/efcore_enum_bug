# efcore_enum_bug

The `CreatePagedResult` in this repo should represent the following sql query:

```sql
SELECT [o2].[Id], (
          SELECT TOP(1) [o].[Price]
          FROM [OrderItems] AS [o]
          WHERE ([o2].[Id] = [o].[OrderId]) AND ([o].[Type] = 'MyType1')) AS [SpecialSum]
      FROM (
          SELECT TOP(1) [o0].[Id]
          FROM [Orders] AS [o0]
          WHERE EXISTS (
              SELECT 1
              FROM [OrderItems] AS [o1]
              WHERE [o0].[Id] = [o1].[OrderId])
          ORDER BY [o0].[Id]
      ) AS [t]
      INNER JOIN [Orders] AS [o2] ON [t].[Id] = [o2].[Id]
      ORDER BY [t].[Id]
```

This works fine as long as `MyType1` isn't an ***enum*** and you don't have enum ***string conversion*** enabled - otherwise ef core will generate the following:

```sql
-- Failed executing DbCommand (4ms) [Parameters=[@__p_0='MyType1' (Nullable = false) (Size = 4000)], CommandType='Text', CommandTimeout='30'
SELECT [o2].[Id], (
          SELECT TOP(1) [o].[Price]
          FROM [OrderItems] AS [o]
          WHERE ([o2].[Id] = [o].[OrderId]) AND ([o].[Type] = @__p_0)) AS [SpecialSum]
      FROM (
          SELECT TOP(@__p_0) [o0].[Id]
          FROM [Orders] AS [o0]
          WHERE EXISTS (
              SELECT 1
              FROM [OrderItems] AS [o1]
              WHERE [o0].[Id] = [o1].[OrderId])
          ORDER BY [o0].[Id]
      ) AS [t]
      INNER JOIN [Orders] AS [o2] ON [t].[Id] = [o2].[Id]
      ORDER BY [t].[Id]
      
-- Microsoft.Data.SqlClient.SqlException (0x80131904): The number of rows provided for a TOP or FETCH clauses row count parameter must be an integer.
```

The query above fails because ef tries to use the string parameter `@__p_0` in the `SELECT TOP(...)` expression. This happens everytime the enum value matches the integer value passed to the `.Take(...)` method.

The same is true if the enum value matches something passed in the `.Skip(...)` expression:

```sql
-- Failed executing DbCommand (2ms) [Parameters=[@__p_1='MyType1' (Nullable = false) (Size = 4000), @__p_0='3'], CommandType='Text', CommandTimeout='30']
      SELECT [o2].[Id], (
          SELECT TOP(1) [o].[Price]
          FROM [OrderItems] AS [o]
          WHERE ([o2].[Id] = [o].[OrderId]) AND ([o].[Type] = @__p_1)) AS [SpecialSum]
      FROM (
          SELECT [t].[Id]
          FROM (
              SELECT TOP(@__p_0) [o0].[Id]
              FROM [Orders] AS [o0]
              WHERE EXISTS (
                  SELECT 1
                  FROM [OrderItems] AS [o1]
                  WHERE [o0].[Id] = [o1].[OrderId])
              ORDER BY [o0].[Id]
          ) AS [t]
          ORDER BY [t].[Id]
          OFFSET @__p_1 ROWS
      ) AS [t0]
      INNER JOIN [Orders] AS [o2] ON [t0].[Id] = [o2].[Id]
      ORDER BY [t0].[Id]
      
-- fail: Microsoft.EntityFrameworkCore.Query[10100]
--      An exception occurred while iterating over the results of a query for context type 'ef_bug.DemoContext'.
--      Microsoft.Data.SqlClient.SqlException (0x80131904): The number of rows provided for a OFFSET clause must be an integer.
```

