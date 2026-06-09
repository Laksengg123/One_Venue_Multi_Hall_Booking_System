using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace VenueBookingSystem.Storage
{
   
    public class DatabaseContext : IDisposable
    {
        private readonly string _connectionString;
        private bool _disposed = false;

        public static DatabaseContext? Instance { get; private set; }

        public static void Initialize(IConfiguration configuration)
        {
            if (Instance == null)
            {
                Instance = new DatabaseContext(configuration);
            }
        }
        public DatabaseContext(IConfiguration configuration)
        {
            string? connStr = configuration.GetConnectionString("DefaultConnection");
            connStr ??= "Server=LAKSHAYA;Database=VenueBookingDB;Trusted_Connection=True;TrustServerCertificate=True;";
            _connectionString = connStr;
        }

        public DatabaseContext(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be empty.");
            _connectionString = connectionString;
        }

        
        ~DatabaseContext()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    
                }
                _disposed = true;
            }
        }


        public async Task<SqlConnection> CreateConnectionAsync()
        {
            var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            return conn;
        }
        public string ConnectionString => _connectionString;


        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (var conn = await CreateConnectionAsync())
                {
                    return conn.State == System.Data.ConnectionState.Open;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
