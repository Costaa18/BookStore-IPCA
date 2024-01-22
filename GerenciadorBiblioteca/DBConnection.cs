using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerenciadorBiblioteca
{
    public static class DBConnection
    {
        public static SqlConnection GetConnection()
        {
            //string dataSource = @"localhost\SQLSEVER";
            //string database = "gerenciador_biblioteca";
            //string connString = $"Data Source={dataSource};Initial Catalog={database};Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            string connString = "Data Source=Costa-Desktop\\SQLSERVER;Initial Catalog=gerenciador_biblioteca;Integrated Security=True";
            return new SqlConnection(connString);
        }
    }
}

