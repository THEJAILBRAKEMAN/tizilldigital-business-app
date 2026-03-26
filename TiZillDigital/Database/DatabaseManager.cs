using System.Data;
using System.Text;
using Microsoft.Data.Sqlite;
using TiZillDigital.Helpers;
using TiZillDigital.Models;

namespace TiZillDigital.Database;

public sealed class DatabaseManager : IDisposable
{
    private readonly SqliteConnection _connection;

    public DatabaseManager(string dbPath)
    {
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        using var pragma = _connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL;";
        pragma.ExecuteNonQuery();
        RunMigrations();
    }

    private void RunMigrations()
    {
        foreach (var sql in new[] { Schema.CreateCreditSales, Schema.CreatePayments, Schema.CreateExpenditure, Schema.CreateKeyOrders, Schema.CreateGames, Schema.CreateGameKeys, Schema.CreateCustomers, Schema.CreateSettings })
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        // Add customer_id columns to existing tables if they don't exist
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE credit_sales ADD COLUMN customer_id TEXT REFERENCES customers(id);";
            cmd.ExecuteNonQuery();
        }
        catch { /* Column already exists */ }

        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE key_orders ADD COLUMN customer_id TEXT REFERENCES customers(id);";
            cmd.ExecuteNonQuery();
        }
        catch { /* Column already exists */ }

        var seeds = new Dictionary<string, string>
        {
            ["business_name"] = "TI ZILL DIGITAL",
            ["address"] = "Mauritius",
            ["phone"] = "",
            ["printer_type"] = "USB",
            ["printer_usb_vid"] = "",
            ["printer_usb_pid"] = "",
            ["printer_serial_port"] = "COM1",
            ["printer_serial_baud"] = "9600",
            ["printer_network_ip"] = "",
            ["printer_network_port"] = "9100",
            ["printer_windows_name"] = "",
            ["currency"] = "MUR",
            ["receipt_footer"] = "Thank you for your purchase!",
            ["receipt_show_logo"] = "false"
        };

        foreach (var kv in seeds)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO settings(key, value) VALUES(@k, @v);";
            cmd.Parameters.AddWithValue("@k", kv.Key);
            cmd.Parameters.AddWithValue("@v", kv.Value);
            cmd.ExecuteNonQuery();
        }
    }

    public List<CreditSale> GetCreditSales()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM credit_sales ORDER BY sale_date DESC, created_at DESC;";
        using var r = cmd.ExecuteReader();
        var list = new List<CreditSale>();
        while (r.Read())
        {
            list.Add(new CreditSale
            {
                Id = r.GetString(0), Customer = r.GetString(1), Phone = r.IsDBNull(2) ? "" : r.GetString(2),
                Item = r.IsDBNull(3) ? "" : r.GetString(3), Amount = r.GetDecimal(4), Paid = r.GetDecimal(5),
                SaleDate = r.GetString(6), Notes = r.IsDBNull(7) ? "" : r.GetString(7), CreatedAt = r.IsDBNull(8) ? "" : r.GetString(8)
            });
        }
        return list;
    }

    public void AddCreditSale(CreditSale sale)
    {
        sale.Id = string.IsNullOrWhiteSpace(sale.Id) ? IdHelper.NewId() : sale.Id;
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"INSERT INTO credit_sales(id,customer,phone,item,amount,paid,sale_date,notes,created_at)
VALUES(@id,@customer,@phone,@item,@amount,@paid,@sale_date,@notes,@created_at);";
        BindCreditSale(cmd, sale);
        cmd.ExecuteNonQuery();
    }

    public void UpdateCreditSale(CreditSale sale)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"UPDATE credit_sales SET customer=@customer,phone=@phone,item=@item,amount=@amount,paid=@paid,sale_date=@sale_date,notes=@notes WHERE id=@id;";
        BindCreditSale(cmd, sale, false);
        cmd.Parameters.AddWithValue("@id", sale.Id);
        cmd.ExecuteNonQuery();
    }

    private static void BindCreditSale(SqliteCommand cmd, CreditSale sale, bool includeCreate = true)
    {
        cmd.Parameters.AddWithValue("@id", sale.Id);
        cmd.Parameters.AddWithValue("@customer", sale.Customer);
        cmd.Parameters.AddWithValue("@phone", sale.Phone);
        cmd.Parameters.AddWithValue("@item", sale.Item);
        cmd.Parameters.AddWithValue("@amount", sale.Amount);
        cmd.Parameters.AddWithValue("@paid", sale.Paid);
        cmd.Parameters.AddWithValue("@sale_date", sale.SaleDate);
        cmd.Parameters.AddWithValue("@notes", sale.Notes);
        if (includeCreate) cmd.Parameters.AddWithValue("@created_at", DateTime.UtcNow.ToString("s"));
    }

    public void DeleteCreditSale(string id)
    {
        using var cmd = _connection.CreateCommand(); cmd.CommandText = "DELETE FROM credit_sales WHERE id=@id"; cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery();
    }

    public void RecordPayment(Payment payment)
    {
        payment.Id = IdHelper.NewId();
        using var tx = _connection.BeginTransaction();
        using var p = _connection.CreateCommand();
        p.Transaction = tx;
        p.CommandText = "INSERT INTO payments(id,credit_id,amount,paid_date,notes,created_at) VALUES(@id,@c,@a,@d,@n,@t)";
        p.Parameters.AddWithValue("@id", payment.Id); p.Parameters.AddWithValue("@c", payment.CreditId); p.Parameters.AddWithValue("@a", payment.Amount);
        p.Parameters.AddWithValue("@d", payment.PaidDate); p.Parameters.AddWithValue("@n", payment.Notes); p.Parameters.AddWithValue("@t", DateTime.UtcNow.ToString("s")); p.ExecuteNonQuery();
        using var u = _connection.CreateCommand();
        u.Transaction = tx;
        u.CommandText = "UPDATE credit_sales SET paid = paid + @a WHERE id=@id";
        u.Parameters.AddWithValue("@a", payment.Amount); u.Parameters.AddWithValue("@id", payment.CreditId); u.ExecuteNonQuery();
        tx.Commit();
    }

    public List<Expenditure> GetExpenditures() => QueryList("SELECT * FROM expenditure ORDER BY expense_date DESC", r => new Expenditure { Id = r.GetString(0), Description = r.GetString(1), Amount = r.GetDecimal(2), Category = r.GetString(3), ExpenseDate = r.GetString(4), Notes = r.IsDBNull(5) ? "" : r.GetString(5), CreatedAt = r.IsDBNull(6) ? "" : r.GetString(6) });

    public void AddExpenditure(Expenditure e)
    {
        e.Id = string.IsNullOrWhiteSpace(e.Id) ? IdHelper.NewId() : e.Id;
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO expenditure(id,description,amount,category,expense_date,notes,created_at) VALUES(@id,@d,@a,@c,@dt,@n,@t)";
        cmd.Parameters.AddWithValue("@id", e.Id); cmd.Parameters.AddWithValue("@d", e.Description); cmd.Parameters.AddWithValue("@a", e.Amount); cmd.Parameters.AddWithValue("@c", e.Category); cmd.Parameters.AddWithValue("@dt", e.ExpenseDate); cmd.Parameters.AddWithValue("@n", e.Notes); cmd.Parameters.AddWithValue("@t", DateTime.UtcNow.ToString("s"));
        cmd.ExecuteNonQuery();
    }
    public void UpdateExpenditure(Expenditure e)
    {
        using var cmd = _connection.CreateCommand(); cmd.CommandText = "UPDATE expenditure SET description=@d,amount=@a,category=@c,expense_date=@dt,notes=@n WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", e.Id); cmd.Parameters.AddWithValue("@d", e.Description); cmd.Parameters.AddWithValue("@a", e.Amount); cmd.Parameters.AddWithValue("@c", e.Category); cmd.Parameters.AddWithValue("@dt", e.ExpenseDate); cmd.Parameters.AddWithValue("@n", e.Notes); cmd.ExecuteNonQuery();
    }
    public void DeleteExpenditure(string id) { using var cmd = _connection.CreateCommand(); cmd.CommandText = "DELETE FROM expenditure WHERE id=@id"; cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); }

    public List<KeyOrder> GetKeyOrders() => QueryList("SELECT * FROM key_orders ORDER BY order_date DESC", r => new KeyOrder { Id = r.GetString(0), Customer = r.GetString(1), Phone = r.IsDBNull(2) ? "" : r.GetString(2), Email = r.IsDBNull(3) ? "" : r.GetString(3), Game = r.GetString(4), Platform = r.GetString(5), Price = r.GetDecimal(6), ProductKey = r.IsDBNull(7) ? "" : r.GetString(7), Status = r.GetString(8), OrderDate = r.GetString(9), Notes = r.IsDBNull(10) ? "" : r.GetString(10), CreatedAt = r.IsDBNull(11) ? "" : r.GetString(11) });
    public List<KeyOrder> GetRecentKeyOrders(int count = 5) => QueryList($"SELECT * FROM key_orders ORDER BY created_at DESC LIMIT {count}", r => new KeyOrder { Id = r.GetString(0), Customer = r.GetString(1), Phone = r.IsDBNull(2) ? "" : r.GetString(2), Email = r.IsDBNull(3) ? "" : r.GetString(3), Game = r.GetString(4), Platform = r.GetString(5), Price = r.GetDecimal(6), ProductKey = r.IsDBNull(7) ? "" : r.GetString(7), Status = r.GetString(8), OrderDate = r.GetString(9), Notes = r.IsDBNull(10) ? "" : r.GetString(10), CreatedAt = r.IsDBNull(11) ? "" : r.GetString(11) });
    public void AddKeyOrder(KeyOrder k) { k.Id = string.IsNullOrWhiteSpace(k.Id) ? IdHelper.NewId() : k.Id; using var cmd = _connection.CreateCommand(); cmd.CommandText = "INSERT INTO key_orders(id,customer,phone,email,game,platform,price,product_key,status,order_date,notes,created_at) VALUES(@id,@c,@p,@e,@g,@pl,@pr,@k,@s,@d,@n,@t)"; BindKeyOrder(cmd, k, true); cmd.ExecuteNonQuery(); }
    public void UpdateKeyOrder(KeyOrder k) { using var cmd = _connection.CreateCommand(); cmd.CommandText = "UPDATE key_orders SET customer=@c,phone=@p,email=@e,game=@g,platform=@pl,price=@pr,product_key=@k,status=@s,order_date=@d,notes=@n WHERE id=@id"; BindKeyOrder(cmd, k, false); cmd.Parameters.AddWithValue("@id", k.Id); cmd.ExecuteNonQuery(); }
    private static void BindKeyOrder(SqliteCommand cmd, KeyOrder k, bool includeCreate)
    {
        cmd.Parameters.AddWithValue("@id", k.Id); cmd.Parameters.AddWithValue("@c", k.Customer); cmd.Parameters.AddWithValue("@p", k.Phone); cmd.Parameters.AddWithValue("@e", k.Email); cmd.Parameters.AddWithValue("@g", k.Game); cmd.Parameters.AddWithValue("@pl", k.Platform); cmd.Parameters.AddWithValue("@pr", k.Price); cmd.Parameters.AddWithValue("@k", k.ProductKey); cmd.Parameters.AddWithValue("@s", k.Status); cmd.Parameters.AddWithValue("@d", k.OrderDate); cmd.Parameters.AddWithValue("@n", k.Notes);
        if (includeCreate) cmd.Parameters.AddWithValue("@t", DateTime.UtcNow.ToString("s"));
    }
    public void DeleteKeyOrder(string id) { using var cmd = _connection.CreateCommand(); cmd.CommandText = "DELETE FROM key_orders WHERE id=@id"; cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); }
    public void MarkDelivered(string id) { using var cmd = _connection.CreateCommand(); cmd.CommandText = "UPDATE key_orders SET status='Delivered', notes=COALESCE(notes,'') || ' Delivered at: ' || @d WHERE id=@id"; cmd.Parameters.AddWithValue("@d", DateTime.Now.ToString("s")); cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); }

    public List<Game> GetGames() => QueryList("SELECT * FROM games ORDER BY title", r => new Game { Id = r.GetString(0), Title = r.GetString(1), Platform = r.GetString(2), Genre = r.GetString(3), CostPrice = r.GetDecimal(4), SellPrice = r.GetDecimal(5), Notes = r.IsDBNull(6) ? "" : r.GetString(6), AddedDate = r.GetString(7), CreatedAt = r.IsDBNull(8) ? "" : r.GetString(8) });
    public void AddGame(Game g) { g.Id = string.IsNullOrWhiteSpace(g.Id) ? IdHelper.NewId() : g.Id; using var cmd = _connection.CreateCommand(); cmd.CommandText = "INSERT INTO games(id,title,platform,genre,cost_price,sell_price,notes,added_date,created_at) VALUES(@id,@t,@p,@g,@c,@s,@n,@d,@cr)"; cmd.Parameters.AddWithValue("@id", g.Id); cmd.Parameters.AddWithValue("@t", g.Title); cmd.Parameters.AddWithValue("@p", g.Platform); cmd.Parameters.AddWithValue("@g", g.Genre); cmd.Parameters.AddWithValue("@c", g.CostPrice); cmd.Parameters.AddWithValue("@s", g.SellPrice); cmd.Parameters.AddWithValue("@n", g.Notes); cmd.Parameters.AddWithValue("@d", g.AddedDate); cmd.Parameters.AddWithValue("@cr", DateTime.UtcNow.ToString("s")); cmd.ExecuteNonQuery(); }
    public void UpdateGame(Game g) { using var cmd = _connection.CreateCommand(); cmd.CommandText = "UPDATE games SET title=@t,platform=@p,genre=@g,cost_price=@c,sell_price=@s,notes=@n,added_date=@d WHERE id=@id"; cmd.Parameters.AddWithValue("@id", g.Id); cmd.Parameters.AddWithValue("@t", g.Title); cmd.Parameters.AddWithValue("@p", g.Platform); cmd.Parameters.AddWithValue("@g", g.Genre); cmd.Parameters.AddWithValue("@c", g.CostPrice); cmd.Parameters.AddWithValue("@s", g.SellPrice); cmd.Parameters.AddWithValue("@n", g.Notes); cmd.Parameters.AddWithValue("@d", g.AddedDate); cmd.ExecuteNonQuery(); }
    public void DeleteGame(string id) { using var cmd = _connection.CreateCommand(); cmd.CommandText = "DELETE FROM games WHERE id=@id"; cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); }

    public List<GameKey> GetGameKeys(string gameId) => QueryList("SELECT * FROM game_keys WHERE game_id=@id ORDER BY added_date DESC", r => new GameKey { Id = r.GetString(0), GameId = r.GetString(1), ProductKey = r.GetString(2), Status = r.GetString(3), AddedDate = r.GetString(4), SoldDate = r.IsDBNull(5) ? null : r.GetString(5), CreatedAt = r.IsDBNull(6) ? "" : r.GetString(6) }, ("@id", gameId));
    public void AddGameKeys(string gameId, IEnumerable<string> keys)
    {
        using var tx = _connection.BeginTransaction();
        foreach (var key in keys.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()))
        {
            using var cmd = _connection.CreateCommand(); cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO game_keys(id,game_id,product_key,status,added_date,created_at) VALUES(@id,@g,@k,'Available',@d,@c)";
            cmd.Parameters.AddWithValue("@id", IdHelper.NewId()); cmd.Parameters.AddWithValue("@g", gameId); cmd.Parameters.AddWithValue("@k", key); cmd.Parameters.AddWithValue("@d", FormatHelper.Today()); cmd.Parameters.AddWithValue("@c", DateTime.UtcNow.ToString("s"));
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }
    public void MarkGameKeySold(string keyId) { using var cmd = _connection.CreateCommand(); cmd.CommandText = "UPDATE game_keys SET status='Sold', sold_date=@d WHERE id=@id"; cmd.Parameters.AddWithValue("@d", FormatHelper.Today()); cmd.Parameters.AddWithValue("@id", keyId); cmd.ExecuteNonQuery(); }
    public void DeleteGameKey(string keyId) { using var cmd = _connection.CreateCommand(); cmd.CommandText = "DELETE FROM game_keys WHERE id=@id"; cmd.Parameters.AddWithValue("@id", keyId); cmd.ExecuteNonQuery(); }

    public decimal SumOutstandingCredit() => ScalarDecimal("SELECT COALESCE(SUM(amount - paid),0) FROM credit_sales");
    public decimal SumExpenditure() => ScalarDecimal("SELECT COALESCE(SUM(amount),0) FROM expenditure");
    public int CountDeliveredKeys() => ScalarInt("SELECT COUNT(*) FROM key_orders WHERE status='Delivered'");
    public int CountKeysInStock() => ScalarInt("SELECT COUNT(*) FROM game_keys WHERE status='Available'");

    public Dictionary<string, string> GetSettings()
    {
        using var cmd = _connection.CreateCommand(); cmd.CommandText = "SELECT key, value FROM settings";
        using var r = cmd.ExecuteReader(); var d = new Dictionary<string, string>();
        while (r.Read()) d[r.GetString(0)] = r.IsDBNull(1) ? "" : r.GetString(1);
        return d;
    }

    public void SaveSetting(string key, string value)
    {
        using var cmd = _connection.CreateCommand(); cmd.CommandText = "INSERT INTO settings(key,value) VALUES(@k,@v) ON CONFLICT(key) DO UPDATE SET value=@v";
        cmd.Parameters.AddWithValue("@k", key); cmd.Parameters.AddWithValue("@v", value); cmd.ExecuteNonQuery();
    }

    public List<Customer> GetCustomers(string searchTerm = "")
    {
        var sql = "SELECT id, customer_code, name, phone, email, address, notes, created_at, updated_at FROM customers WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            sql += @" AND (customer_code LIKE @search OR name LIKE @search OR phone LIKE @search OR email LIKE @search)";
        }
        sql += " ORDER BY created_at DESC";

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            cmd.Parameters.AddWithValue("@search", $"%{searchTerm}%");
        }

        using var r = cmd.ExecuteReader();
        var list = new List<Customer>();
        while (r.Read())
        {
            list.Add(new Customer
            {
                Id = r.GetString(0),
                CustomerCode = r.GetString(1),
                Name = r.GetString(2),
                Phone = r.IsDBNull(3) ? null : r.GetString(3),
                Email = r.IsDBNull(4) ? null : r.GetString(4),
                Address = r.IsDBNull(5) ? null : r.GetString(5),
                Notes = r.IsDBNull(6) ? null : r.GetString(6),
                CreatedAt = r.IsDBNull(7) ? string.Empty : r.GetString(7),
                UpdatedAt = r.IsDBNull(8) ? string.Empty : r.GetString(8)
            });
        }
        return list;
    }

    public Customer? GetCustomerById(string id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, customer_code, name, phone, email, address, notes, created_at, updated_at FROM customers WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        if (r.Read())
        {
            return new Customer
            {
                Id = r.GetString(0),
                CustomerCode = r.GetString(1),
                Name = r.GetString(2),
                Phone = r.IsDBNull(3) ? null : r.GetString(3),
                Email = r.IsDBNull(4) ? null : r.GetString(4),
                Address = r.IsDBNull(5) ? null : r.GetString(5),
                Notes = r.IsDBNull(6) ? null : r.GetString(6),
                CreatedAt = r.IsDBNull(7) ? string.Empty : r.GetString(7),
                UpdatedAt = r.IsDBNull(8) ? string.Empty : r.GetString(8)
            };
        }
        return null;
    }

    public Customer? GetCustomerByCode(string code)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, customer_code, name, phone, email, address, notes, created_at, updated_at FROM customers WHERE customer_code=@code";
        cmd.Parameters.AddWithValue("@code", code);
        using var r = cmd.ExecuteReader();
        if (r.Read())
        {
            return new Customer
            {
                Id = r.GetString(0),
                CustomerCode = r.GetString(1),
                Name = r.GetString(2),
                Phone = r.IsDBNull(3) ? null : r.GetString(3),
                Email = r.IsDBNull(4) ? null : r.GetString(4),
                Address = r.IsDBNull(5) ? null : r.GetString(5),
                Notes = r.IsDBNull(6) ? null : r.GetString(6),
                CreatedAt = r.IsDBNull(7) ? string.Empty : r.GetString(7),
                UpdatedAt = r.IsDBNull(8) ? string.Empty : r.GetString(8)
            };
        }
        return null;
    }

    public string AddCustomer(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Id))
            customer.Id = IdHelper.NewId();

        var customerCode = GenerateNextCustomerCode();
        customer.CustomerCode = customerCode;
        var now = DateTime.UtcNow.ToString("s");
        customer.CreatedAt = now;
        customer.UpdatedAt = now;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
INSERT INTO customers(id, customer_code, name, phone, email, address, notes, created_at, updated_at)
VALUES(@id, @code, @name, @phone, @email, @address, @notes, @created_at, @updated_at)";
        cmd.Parameters.AddWithValue("@id", customer.Id);
        cmd.Parameters.AddWithValue("@code", customer.CustomerCode);
        cmd.Parameters.AddWithValue("@name", customer.Name);
        cmd.Parameters.AddWithValue("@phone", customer.Phone ?? "");
        cmd.Parameters.AddWithValue("@email", customer.Email ?? "");
        cmd.Parameters.AddWithValue("@address", customer.Address ?? "");
        cmd.Parameters.AddWithValue("@notes", customer.Notes ?? "");
        cmd.Parameters.AddWithValue("@created_at", customer.CreatedAt);
        cmd.Parameters.AddWithValue("@updated_at", customer.UpdatedAt);
        cmd.ExecuteNonQuery();

        return customer.CustomerCode;
    }

    public void UpdateCustomer(Customer customer)
    {
        customer.UpdatedAt = DateTime.UtcNow.ToString("s");

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
UPDATE customers 
SET name=@name, phone=@phone, email=@email, address=@address, notes=@notes, updated_at=@updated_at 
WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", customer.Id);
        cmd.Parameters.AddWithValue("@name", customer.Name);
        cmd.Parameters.AddWithValue("@phone", customer.Phone ?? "");
        cmd.Parameters.AddWithValue("@email", customer.Email ?? "");
        cmd.Parameters.AddWithValue("@address", customer.Address ?? "");
        cmd.Parameters.AddWithValue("@notes", customer.Notes ?? "");
        cmd.Parameters.AddWithValue("@updated_at", customer.UpdatedAt);
        cmd.ExecuteNonQuery();
    }

    public void DeleteCustomer(string id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM customers WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public string GenerateNextCustomerCode()
    {
        using var tx = _connection.BeginTransaction();
        using var cmd = _connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT MAX(CAST(SUBSTR(customer_code, 4) AS INTEGER)) FROM customers";
        var result = cmd.ExecuteScalar();
        int nextNumber = 1;
        if (result != null && result != DBNull.Value)
        {
            nextNumber = Convert.ToInt32(result) + 1;
        }

        string code = nextNumber > 999 ? $"CUS{nextNumber}" : $"CUS{nextNumber:000}";
        tx.Commit();
        return code;
    }

    public int CountOutstandingByCustomer(string customerId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM credit_sales WHERE customer_id=@cid AND (amount - paid) > 0";
        cmd.Parameters.AddWithValue("@cid", customerId);
        return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
    }

    public int CountOrdersByCustomer(string customerId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM key_orders WHERE customer_id=@cid";
        cmd.Parameters.AddWithValue("@cid", customerId);
        return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
    }

    public void ExportToCsv(string tableName, string filePath)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"SELECT * FROM {tableName}";
        using var reader = cmd.ExecuteReader();
        using var sw = new StreamWriter(filePath, false, new UTF8Encoding(true));
        var headers = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
        sw.WriteLine(string.Join(",", headers));
        while (reader.Read())
        {
            var vals = new string[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var v = reader.IsDBNull(i) ? "" : reader.GetValue(i)?.ToString() ?? "";
                vals[i] = $"\"{v.Replace("\"", "\"\"")}\"";
            }
            sw.WriteLine(string.Join(",", vals));
        }
    }

    private List<T> QueryList<T>(string sql, Func<SqliteDataReader, T> map, params (string key, object value)[] args)
    {
        using var cmd = _connection.CreateCommand(); cmd.CommandText = sql;
        foreach (var a in args) cmd.Parameters.AddWithValue(a.key, a.value);
        using var r = cmd.ExecuteReader(); var list = new List<T>();
        while (r.Read()) list.Add(map(r));
        return list;
    }

    private decimal ScalarDecimal(string sql) { using var cmd = _connection.CreateCommand(); cmd.CommandText = sql; return Convert.ToDecimal(cmd.ExecuteScalar() ?? 0m); }
    private int ScalarInt(string sql) { using var cmd = _connection.CreateCommand(); cmd.CommandText = sql; return Convert.ToInt32(cmd.ExecuteScalar() ?? 0); }

    public void Dispose() => _connection.Dispose();
}
