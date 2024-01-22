using BetterConsoleTables;
using ConsoleTools;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerenciadorBiblioteca.UtilizadoresLogic
{
    public static class Repositor
    {
        public static void listarLivrosDisponiveis()
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    // Obter apenas os livros disponíveis
                    string queryObterLivros = "SELECT Codigo, Titulo, Autor, Stock, Preco FROM Livros WHERE Stock > 0";
                    using (SqlCommand obterLivrosCommand = new SqlCommand(queryObterLivros, connection))
                    {
                        using (SqlDataReader livrosReader = obterLivrosCommand.ExecuteReader())
                        {
                            Console.WriteLine("Lista de Livros Disponíveis:");

                            var table = new Table("Código", "Título", "Autor", "Stock", "Preço");

                            while (livrosReader.Read())
                            {
                                string codigo = livrosReader["Codigo"].ToString();
                                string titulo = livrosReader["Titulo"].ToString();
                                string autor = livrosReader["Autor"].ToString();
                                int stock = Convert.ToInt32(livrosReader["Stock"]);
                                decimal preco = Convert.ToDecimal(livrosReader["Preco"]);

                                table.AddRow(codigo, titulo, autor, stock.ToString(), preco.ToString("C"));
                            }

                            Console.WriteLine(table.ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro ao listar livros disponíveis: " + e.Message);
                }
                finally
                {
                    connection.Close();
                }

                Console.ReadKey();
                Console.Clear();
            }
        }

        public static void adicionarLivro()
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    // Obter todos os livros
                    string queryLivros = "SELECT Codigo, Titulo, Autor, Stock FROM Livros";
                    using (SqlCommand livrosCommand = new SqlCommand(queryLivros, connection))
                    {
                        using (SqlDataReader livrosReader = livrosCommand.ExecuteReader())
                        {
                            Console.WriteLine("Escolha o livro para adicionar stock:");

                            var menuLivros = new ConsoleMenu()
                                .Configure(config => { config.WriteHeaderAction = () => Console.WriteLine($"Lista de Livros" + "\nEscolha uma opção:"); });

                            Dictionary<string, string> livrosDisponiveis = new Dictionary<string, string>();

                            while (livrosReader.Read())
                            {
                                string codigoLivro = livrosReader["Codigo"].ToString();
                                string tituloLivro = livrosReader["Titulo"].ToString();
                                int stockLivro = Convert.ToInt32(livrosReader["Stock"]);

                                livrosDisponiveis.Add(codigoLivro, $"{tituloLivro} (Stock: {stockLivro})");

                                menuLivros.Add($"{codigoLivro}. {tituloLivro} (Stock: {stockLivro})", () => AdicionarStockLivro(codigoLivro, connection));
                            }

                            livrosReader.Close();

                            menuLivros.Add("Cancelar Operação", ConsoleMenu.Close);
                            menuLivros.Show();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro ao adicionar stock: " + e.Message);
                }
                Console.ReadKey();
            }
        }

        private static void AtualizarEstoqueLivro(SqlConnection connection, string codigoLivro, int stock, string operacao)
        {
            // Atualizar o estoque após o empréstimo
            string queryAtualizarEstoque = "UPDATE Livros SET Stock = Stock " + operacao + " @Stock WHERE Codigo = @CodigoLivro";

            using (SqlCommand atualizarEstoqueCommand = new SqlCommand(queryAtualizarEstoque, connection))
            {
                atualizarEstoqueCommand.Parameters.AddWithValue("@Stock", stock);
                atualizarEstoqueCommand.Parameters.AddWithValue("@CodigoLivro", codigoLivro);
                atualizarEstoqueCommand.ExecuteNonQuery();
            }
        }

        private static void AdicionarStockLivro(string codigoLivro, SqlConnection connection)
        {
            Console.Write($"Digite a quantidade de stock a ser adicionada para o livro {codigoLivro}: ");
            if (int.TryParse(Console.ReadLine(), out int quantidade))
            {
                try
                {
                    // Atualizar o estoque
                    AtualizarEstoqueLivro(connection, codigoLivro, quantidade, "+");

                    Console.WriteLine("Stock adicionado com sucesso!");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro ao adicionar stock: " + e.Message);
                }
            }
            else
            {
                Console.WriteLine("Quantidade inválida. Operação cancelada.");
            }

            Console.ReadKey();
        }

        public static void consultarStock()
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    // Consulta SQL para listar todos os Autores
                    string sqlQuery = "SELECT DISTINCT Codigo FROM Livros ORDER BY Codigo ASC";
                    using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // Se houver Autores, mostra a lista
                            if (reader.HasRows)
                            {
                                List<string> livros = new List<string>();

                                while (reader.Read())
                                {
                                    string livroCodigo = reader["Codigo"].ToString();
                                    livros.Add(livroCodigo);
                                }
                                reader.Close();

                                var livrosMenu = new ConsoleMenu().Configure(config => { config.WriteHeaderAction = () => Console.WriteLine($"Lista de Livros" + "\nEscolha uma opção:"); });
                                livrosMenu.Add("Consultar stock geral", () =>
                                {
                                    consultarStockGeral(connection, livrosMenu);
                                    livrosMenu.CloseMenu();
                                });
                                foreach (var livro in livros)
                                {
                                    livrosMenu.Add(livro, () => {
                                        consultarStockPorCodigo(connection, livro, livrosMenu);
                                        livrosMenu.CloseMenu();
                                    });
                                }
                                livrosMenu.Add("Voltar", ConsoleMenu.Close);


                                // Exibir o menu e aguardar a escolha do utilizador
                                livrosMenu.Show();
                            }
                            else
                            {
                                // Se não houver autores, informa o utilizador
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Nenhum autor encontrado.");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro ao listar livros por Autor: " + e.Message);
                }
                finally
                {
                    connection.Close();
                }
                Console.ReadKey();
                Console.Clear();
            }
        }

        private static void consultarStockGeral(SqlConnection connection, ConsoleMenu livrosMenu)
        {


            // Obter informações sobre os livros disponíveis
            string getLivrosQuery = "SELECT Codigo, Titulo, Preco, TaxaIVA, Stock FROM Livros";
            using (SqlCommand getLivrosCommand = new SqlCommand(getLivrosQuery, connection))
            {

                using (SqlDataReader livrosReader = getLivrosCommand.ExecuteReader())
                {
                    var tableLivros = new Table("Código", "Título", "Preço Unitário", "Taxa IVA", "Stock");

                    while (livrosReader.Read())
                    {
                        tableLivros.AddRow(
                            livrosReader["Codigo"].ToString(),
                            livrosReader["Titulo"].ToString(),
                            $"{livrosReader["Preco"]:C}",
                            $"{livrosReader["TaxaIVA"]}%",
                            livrosReader["Stock"].ToString()
                        );
                    }

                    Console.WriteLine("Stock:");
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(tableLivros.ToString());

                }
            }
            Console.ReadKey();
            livrosMenu.CloseMenu();
        }

        private static void consultarStockPorCodigo(SqlConnection connection, string codigo, ConsoleMenu livrosMenu)
        {
            // Obter informações sobre os livros disponíveis
            string getLivrosQuery = "SELECT Codigo, Titulo, Preco, TaxaIVA, Stock FROM Livros WHERE Codigo = @Codigo";

            using (SqlCommand getDetalhesCommand = new SqlCommand(getLivrosQuery, connection))
            {
                getDetalhesCommand.Parameters.AddWithValue("@Codigo", codigo);

                using (SqlDataReader livrosReader = getDetalhesCommand.ExecuteReader())
                {
                    var tableLivros = new Table("Código", "Título", "Preço Unitário", "Taxa IVA", "Stock");

                    while (livrosReader.Read())
                    {
                        tableLivros.AddRow(
                            livrosReader["Codigo"].ToString(),
                            livrosReader["Titulo"].ToString(),
                            $"{livrosReader["Preco"]:C}",
                            $"{livrosReader["TaxaIVA"]}%",
                            livrosReader["Stock"].ToString()
                        );
                    }

                    Console.WriteLine("Stock:");
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(tableLivros.ToString());

                }
            }
            Console.ReadKey();
            livrosMenu.CloseMenu();
        }



    }
}
