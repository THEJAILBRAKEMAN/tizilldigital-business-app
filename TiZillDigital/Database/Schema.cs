namespace TiZillDigital.Database;

public static class Schema
{
    public const string CreateCreditSales = @"
CREATE TABLE IF NOT EXISTS credit_sales (
  id TEXT PRIMARY KEY,
  customer TEXT NOT NULL,
  phone TEXT,
  item TEXT,
  amount REAL NOT NULL DEFAULT 0,
  paid REAL NOT NULL DEFAULT 0,
  sale_date TEXT NOT NULL,
  notes TEXT,
  created_at TEXT
);";

    public const string CreatePayments = @"
CREATE TABLE IF NOT EXISTS payments (
  id TEXT PRIMARY KEY,
  credit_id TEXT NOT NULL,
  amount REAL NOT NULL,
  paid_date TEXT NOT NULL,
  notes TEXT,
  created_at TEXT,
  FOREIGN KEY(credit_id) REFERENCES credit_sales(id) ON DELETE CASCADE
);";

    public const string CreateExpenditure = @"
CREATE TABLE IF NOT EXISTS expenditure (
  id TEXT PRIMARY KEY,
  description TEXT NOT NULL,
  amount REAL NOT NULL,
  category TEXT NOT NULL DEFAULT 'Misc',
  expense_date TEXT NOT NULL,
  notes TEXT,
  created_at TEXT
);";

    public const string CreateKeyOrders = @"
CREATE TABLE IF NOT EXISTS key_orders (
  id TEXT PRIMARY KEY,
  customer TEXT NOT NULL,
  phone TEXT,
  email TEXT,
  game TEXT NOT NULL,
  platform TEXT NOT NULL DEFAULT 'Steam',
  price REAL NOT NULL DEFAULT 0,
  product_key TEXT,
  status TEXT NOT NULL DEFAULT 'Pending',
  order_date TEXT NOT NULL,
  notes TEXT,
  created_at TEXT
);";

    public const string CreateGames = @"
CREATE TABLE IF NOT EXISTS games (
  id TEXT PRIMARY KEY,
  title TEXT NOT NULL,
  platform TEXT NOT NULL DEFAULT 'Steam',
  genre TEXT NOT NULL DEFAULT 'Action',
  cost_price REAL NOT NULL DEFAULT 0,
  sell_price REAL NOT NULL DEFAULT 0,
  notes TEXT,
  added_date TEXT NOT NULL,
  created_at TEXT
);";

    public const string CreateGameKeys = @"
CREATE TABLE IF NOT EXISTS game_keys (
  id TEXT PRIMARY KEY,
  game_id TEXT NOT NULL,
  product_key TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'Available',
  added_date TEXT NOT NULL,
  sold_date TEXT,
  created_at TEXT,
  FOREIGN KEY(game_id) REFERENCES games(id) ON DELETE CASCADE
);";

    public const string CreateCustomers = @"
CREATE TABLE IF NOT EXISTS customers (
  id TEXT PRIMARY KEY,
  customer_code TEXT NOT NULL UNIQUE,
  name TEXT NOT NULL,
  phone TEXT,
  email TEXT,
  address TEXT,
  notes TEXT,
  created_at TEXT DEFAULT (datetime('now')),
  updated_at TEXT DEFAULT (datetime('now'))
);";

    public const string CreateSettings = @"
CREATE TABLE IF NOT EXISTS settings (
  key TEXT PRIMARY KEY,
  value TEXT
);";
}
