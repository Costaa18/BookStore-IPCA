using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace GerenciadorBiblioteca
{
    public static class login
    {
        public class UserData
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Role { get; set; }
        }


        public static UserData ValidateLogin(string name, string password)
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    // Aqui você pode realizar a lógica de login, como verificar se o utilizador e password são válidos no banco de dados.
                    string sqlQuery = $"SELECT id, name, role FROM users WHERE name = '{name}' AND password = '{password}'";
                    using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                reader.Read();
                                string role = reader.GetString(2);
                                int id = reader.GetInt32(0);

                                switch (role)
                                {
                                    case "G":
                                        role = "Gerente";
                                        break;
                                    case "C":
                                        role = "Caixa";
                                        break;
                                    case "R":
                                        role = "Repositor";
                                        break;
                                } 

                                // Retorna o objeto UserData com os dados do utilizador
                                return new UserData
                                {
                                    Id = id,
                                    Name = name,
                                    Role = role
                                };
                            }
                            else
                            {
                                return null; 
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                    return null;
                }
            }
        }
    }
}
