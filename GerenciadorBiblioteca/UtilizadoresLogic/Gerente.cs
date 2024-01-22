using BetterConsoleTables;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.SqlServer.Server;
using ConsoleTools;

namespace GerenciadorBiblioteca.UtilizadoresLogic
{
    public static class Gerente
    {
        public static void registarLivro(string codigo, string titulo, string autor, string ISBN, string genero, decimal preco, decimal taxaIVA, int stock)
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                SqlTransaction transaction = null;

                try
                {
                    connection.Open();
                    transaction = connection.BeginTransaction();

                    // Verifica se o livro já existe pelo código
                    if (LivroJaExiste(connection, codigo, transaction))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Erro: Já existe um livro com o mesmo código. Por favor, escolha outra opção.");
                        Console.ResetColor();
                        transaction.Rollback();
                        return;
                    }

                    // Se o livro não existe, insere um novo com estoque inicial de 1
                    string insertQuery = "INSERT INTO Livros (Codigo, Titulo, Autor, ISBN, Genero, Preco, TaxaIVA, Stock) " +
                                         "VALUES (@Codigo, @Titulo, @Autor, @ISBN, @Genero, @Preco, @TaxaIVA, @Stock)";

                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection, transaction))
                    {
                        insertCommand.Parameters.AddWithValue("@Codigo", codigo);
                        insertCommand.Parameters.AddWithValue("@Titulo", titulo);
                        insertCommand.Parameters.AddWithValue("@Autor", autor);
                        insertCommand.Parameters.AddWithValue("@ISBN", ISBN);
                        insertCommand.Parameters.AddWithValue("@Genero", genero);
                        insertCommand.Parameters.AddWithValue("@Preco", preco);
                        insertCommand.Parameters.AddWithValue("@TaxaIVA", taxaIVA);
                        insertCommand.Parameters.AddWithValue("@Stock", stock);

                        insertCommand.ExecuteNonQuery();

                        Console.WriteLine("Livro registado com sucesso!");
                        transaction.Commit();
                    }
                }
                catch (SqlException sqlException)
                {
                    Console.WriteLine("Erro ao registar livro: " + sqlException.Message);
                    transaction?.Rollback();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro inesperado ao registar livro: " + e.Message);
                    transaction?.Rollback();
                }
                finally
                {
                    connection.Close();
                    Console.ReadKey();
                    Console.Clear();
                }
            }
        }

        // Método privado para verificar se um livro com o código fornecido já existe na base de dados.
        private static bool LivroJaExiste(SqlConnection connection, string codigo, SqlTransaction transaction)
        {
            // Consulta SQL para contar a quantidade de livros com o código fornecido.
            string checkIfExistsQuery = "SELECT COUNT(*) FROM Livros WHERE Codigo = @Codigo";

            // Utilizando um objeto SqlCommand para executar a consulta, associando-o à transação se fornecida.
            using (SqlCommand checkCommand = new SqlCommand(checkIfExistsQuery, connection, transaction))
            {
                // Adicionando o parâmetro @Codigo à consulta para evitar injeção de SQL.
                checkCommand.Parameters.AddWithValue("@Codigo", codigo);

                // Executando a consulta e obtendo o resultado como um inteiro.
                int existingCount = (int)checkCommand.ExecuteScalar();

                // Retorna verdadeiro se a contagem de livros existentes for maior que zero, indicando que o livro já existe.
                return existingCount > 0;
            }
        }

        public static void atualizarLivro()
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    // Consulta SQL para listar todos os códigos e títulos dos livros
                    string livrosQuery = "SELECT Codigo, Titulo FROM Livros ORDER BY Codigo ASC";

                    // Lista para armazenar os códigos e títulos
                    List<(string Codigo, string Titulo)> livros = new List<(string, string)>();

                    using (SqlCommand livrosCommand = new SqlCommand(livrosQuery, connection))
                    {
                        using (SqlDataReader livrosReader = livrosCommand.ExecuteReader())
                        {
                            // Se houver livros, mostra a lista
                            if (livrosReader.HasRows)
                            {
                                while (livrosReader.Read())
                                {
                                    string codigo = livrosReader["Codigo"].ToString();
                                    string titulo = livrosReader["Titulo"].ToString();
                                    livros.Add((codigo, titulo));
                                    Console.WriteLine($"Código: {codigo}, Título: {titulo}");
                                }
                                livrosReader.Close();

                                // Criar um menu para os livros
                                var livroMenu = new ConsoleMenu()
                                    .Configure(config => { config.WriteHeaderAction = () => Console.WriteLine($"Lista de Livros" + "\nEscolha uma opção:"); });
                                foreach (var livro in livros)
                                {
                                    // Criar uma cópia das variáveis locais para evitar o problema de captura
                                    string codigoLivroLocal = livro.Codigo;
                                    livroMenu.Add($"{livro.Codigo} - {livro.Titulo}", () => {
                                        AtualizarDetalhesLivro(connection, codigoLivroLocal, livroMenu);
                                        livroMenu.CloseMenu();
                                    });
                                }
                                livroMenu.Add("Voltar", ConsoleMenu.Close);

                                // Exibir o menu e aguardar a escolha do utilizador
                                livroMenu.Show();
                            }
                            else
                            {
                                // Se não houver livros, informa o utilizador
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Nenhum livro encontrado.");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro ao listar livros: " + e.Message);
                }
                finally
                {
                    connection.Close();
                }
                Console.ReadKey();
                Console.Clear();
            }
        }

        private static void AtualizarDetalhesLivro(SqlConnection connection, string codigoEscolhido, ConsoleMenu livroMenu)
        {
            // Verifica se o livro existe pelo código
            string checkIfExistsQuery = "SELECT * FROM Livros WHERE Codigo = @Codigo";
            using (SqlCommand checkCommand = new SqlCommand(checkIfExistsQuery, connection))
            {
                checkCommand.Parameters.AddWithValue("@Codigo", codigoEscolhido);
                using (SqlDataReader reader = checkCommand.ExecuteReader())
                {
                    // Se o livro existe, mostra detalhes e permite a atualização
                    if (reader.Read())
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Detalhes do Livro (Código: {codigoEscolhido}):");
                        Console.WriteLine($"Título: {reader["Titulo"]}");
                        Console.WriteLine($"Autor: {reader["Autor"]}");
                        Console.WriteLine($"ISBN: {reader["ISBN"]}");
                        Console.WriteLine($"Género: {reader["Genero"]}");
                        Console.WriteLine($"Preço: {reader["Preco"]}");
                        Console.WriteLine($"Taxa de IVA: {reader["TaxaIVA"]}");
                        Console.WriteLine($"Stock: {reader["Stock"]}");

                        Console.WriteLine("\nAtualize as informações:");

                        Console.Write("Título: ");
                        string novoTitulo = Console.ReadLine();

                        Console.Write("Autor: ");
                        string novoAutor = Console.ReadLine();

                        Console.Write("ISBN: ");
                        string novoISBN = Console.ReadLine();

                        Console.Write("Género: ");
                        string novoGenero = Console.ReadLine();

                        Console.Write("Preço: ");
                        decimal novoPreco;
                        while (!decimal.TryParse(Console.ReadLine(), out novoPreco))
                        {
                            Console.WriteLine("Por favor, insira um valor numérico para o preço.");
                            Console.Write("Preço: ");
                        }

                        Console.Write("Taxa de IVA (6% ou 23%): ");
                        decimal novoTaxaIVA;
                        while (!decimal.TryParse(Console.ReadLine(), out novoTaxaIVA))
                        {
                            Console.WriteLine("Por favor, insira um valor numérico para a taxa de IVA.");
                            Console.Write("Taxa de IVA: ");
                        }

                        Console.Write("Stock: ");
                        int novoStock;
                        while (!int.TryParse(Console.ReadLine(), out novoStock))
                        {
                            Console.WriteLine("Por favor, insira um valor numérico para o stock.");
                            Console.Write("Stock: ");
                        }

                        // Fecha o SqlDataReader antes de realizar a atualização
                        reader.Close();

                        // Lógica para atualizar o livro com as novas informações
                        string updateQuery = "UPDATE Livros SET Titulo = @Titulo, Autor = @Autor, ISBN = @ISBN, Genero = @Genero, Preco = @Preco, TaxaIVA = @TaxaIVA, Stock = @Stock WHERE Codigo = @Codigo";
                        using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@Codigo", codigoEscolhido);
                            updateCommand.Parameters.AddWithValue("@Titulo", novoTitulo);
                            updateCommand.Parameters.AddWithValue("@Autor", novoAutor);
                            updateCommand.Parameters.AddWithValue("@ISBN", novoISBN);
                            updateCommand.Parameters.AddWithValue("@Genero", novoGenero);
                            updateCommand.Parameters.AddWithValue("@Preco", novoPreco);
                            updateCommand.Parameters.AddWithValue("@TaxaIVA", novoTaxaIVA);
                            updateCommand.Parameters.AddWithValue("@Stock", novoStock);

                            int rowsAffected = updateCommand.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                Console.WriteLine("Livro atualizado com sucesso!");
                            }
                            else
                            {
                                Console.WriteLine("Falha ao atualizar o livro. Nenhuma alteração realizada.");
                            }
                        }
                    }
                    else
                    {
                        // Se o livro não existe, informa o utilizador
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Livro com código {codigoEscolhido} não encontrado.");
                    }
                }
            }
            Console.ReadKey();
            livroMenu.CloseMenu();
        }




        // Método para consultar as informações de um livro pelo código
        public static void consultarLivroCodigo()
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    // Consulta SQL para listar todos os códigos de livros
                    string codigosQuery = "SELECT Codigo FROM Livros ORDER BY Codigo ASC";

                    // Lista para armazenar os códigos
                    List<string> codigos = new List<string>();

                    using (SqlCommand codigosCommand = new SqlCommand(codigosQuery, connection))
                    {
                        using (SqlDataReader codigosReader = codigosCommand.ExecuteReader())
                        {
                            // Se houver códigos, mostra a lista
                            if (codigosReader.HasRows)
                            {

                                while (codigosReader.Read())
                                {
                                    string codigo = codigosReader["Codigo"].ToString();
                                    codigos.Add(codigo);
                                    Console.WriteLine(codigo);
                                }
                                codigosReader.Close();

                                // Criar um menu para os códigos
                                var codigoMenu = new ConsoleMenu()
                                    .Configure(config => { config.WriteHeaderAction = () => Console.WriteLine($"Lista de Códigos de Livros!" + "\nEscolha uma opção:"); }) ;

                                foreach (var codigo in codigos)
                                {

                                    codigoMenu.Add(codigo, () => {
                                        MostrarDetalhesLivro(connection, codigo, codigoMenu);
                                        codigoMenu.CloseMenu();
                                    });
                                }
                                codigoMenu.Add("Voltar", ConsoleMenu.Close);

                                // Exibir o menu e aguardar a escolha do utilizador
                                codigoMenu.Show();
                            }
                            else
                            {
                                // Se não houver códigos, informa o utilizador
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Nenhum código de livro encontrado.");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro ao consultar códigos de livro: " + e.Message);
                }
                finally
                {
                    connection.Close();
                }
                Console.ReadKey();
                Console.Clear();
            }
        }

        // Método privado para mostrar os detalhes do livro com base no código escolhido
        private static void MostrarDetalhesLivro(SqlConnection connection, string codigoEscolhido, ConsoleMenu codigoMenu)
        {
            // Consulta SQL para obter os detalhes do livro pelo código
            string detalhesLivroQuery = "SELECT * FROM Livros WHERE Codigo = @Codigo";

            using (SqlCommand detalhesLivroCommand = new SqlCommand(detalhesLivroQuery, connection))
            {
                detalhesLivroCommand.Parameters.AddWithValue("@Codigo", codigoEscolhido);

                using (SqlDataReader detalhesLivroReader = detalhesLivroCommand.ExecuteReader())
                {
                    // Se o livro existir, mostra detalhes
                    if (detalhesLivroReader.Read())
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Detalhes do Livro (Código: {codigoEscolhido}):");
                        Console.WriteLine($"Título: {detalhesLivroReader["Titulo"]}");
                        Console.WriteLine($"Autor: {detalhesLivroReader["Autor"]}");
                        Console.WriteLine($"ISBN: {detalhesLivroReader["ISBN"]}");
                        Console.WriteLine($"Género: {detalhesLivroReader["Genero"]}");
                        Console.WriteLine($"Preço: {detalhesLivroReader["Preco"]}");
                        Console.WriteLine($"Taxa de IVA: {detalhesLivroReader["TaxaIVA"]}");
                        Console.WriteLine($"Stock: {detalhesLivroReader["Stock"]}");
                    }
                    else
                    {
                        // Se o livro não existir, informa o utilizador
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Livro com código {codigoEscolhido} não encontrado.");
                    }
                }
            }
            Console.ReadKey();
            codigoMenu.Show();
        }

        // Método para listar os géneros disponíveis e permitir ao utilizador escolher um para visualizar os livros
        public static void listarLivrosGenero()
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    // Consulta SQL para listar todos os géneros
                    string sqlQuery = "SELECT DISTINCT Genero FROM Livros ORDER BY Genero ASC";
                    using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // Se houver géneros, mostra a lista
                            if (reader.HasRows)
                            {
                                List<string> generos = new List<string>();

                                while (reader.Read())
                                {
                                    string genero = reader["Genero"].ToString();
                                    generos.Add(genero);
                                    //Console.WriteLine(genero);
                                }
                                reader.Close();

                                // Criar um menu para os géneros
                                var generoMenu = new ConsoleMenu()
                                    .Configure(config => { config.WriteHeaderAction = () => Console.WriteLine($"Lista de Géneros" + "\nEscolha uma opção:"); });
                                foreach (var genero in generos)
                                {
                                    // Criar uma cópia da variável local para evitar o problema de captura
                                    string generoLocal = genero;

                                    generoMenu.Add(genero, () => {
                                        MostrarLivrosPorGenero(connection, genero, generoMenu);
                                        generoMenu.CloseMenu();
                                        });
                                }
                                generoMenu.Add("Voltar", ConsoleMenu.Close);
                                

                                // Exibir o menu e aguardar a escolha do utilizador
                                generoMenu.Show();

                            }
                            else
                            {
                                // Se não houver géneros, informa o utilizador
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Nenhum género encontrado.");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro ao listar livros por género: " + e.Message);
                }
                finally
                {
                    connection.Close();
                }
                Console.ReadKey();
            }
        }

        // Método privado para mostrar a lista de livros de um determinado género
        private static void MostrarLivrosPorGenero(SqlConnection connection, string generoEscolhido, ConsoleMenu generoMenu)
        {
            // Consulta SQL para listar os livros do género escolhido
            string livrosPorGeneroQuery = "SELECT * FROM Livros WHERE Genero = @Genero";
            using (SqlCommand livrosPorGeneroCommand = new SqlCommand(livrosPorGeneroQuery, connection))
            {
                livrosPorGeneroCommand.Parameters.AddWithValue("@Genero", generoEscolhido);

                using (SqlDataReader livrosPorGeneroReader = livrosPorGeneroCommand.ExecuteReader())
                {
                    // Se houver livros para o género, mostra a lista
                    if (livrosPorGeneroReader.HasRows)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"\nLista de Livros do Género '{generoEscolhido}':");
                        Console.ResetColor();

                        while (livrosPorGeneroReader.Read())
                        {
                            Console.WriteLine($"Título: {livrosPorGeneroReader["Titulo"]}");
                            Console.WriteLine($"Autor: {livrosPorGeneroReader["Autor"]}");
                            Console.WriteLine($"ISBN: {livrosPorGeneroReader["ISBN"]}");
                            Console.WriteLine($"Preço: {livrosPorGeneroReader["Preco"]}");
                            Console.WriteLine($"Taxa de IVA: {livrosPorGeneroReader["TaxaIVA"]}");
                            Console.WriteLine($"Stock: {livrosPorGeneroReader["Stock"]}\n");
                        }
                    }
                    else
                    {
                        // Se não houver livros para o género, informa o utilizador
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Nenhum livro encontrado para o género '{generoEscolhido}'.");
                    }
                }
            }
            Console.ReadKey();
            generoMenu.Show();
        }

        // Método para listar os autores disponíveis e permitir ao utilizador escolher um para visualizar os livros
        public static void listarLivrosAutor()
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    // Consulta SQL para listar todos os Autores
                    string sqlQuery = "SELECT DISTINCT Autor FROM Livros ORDER BY Autor ASC";
                    using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // Se houver Autores, mostra a lista
                            if (reader.HasRows)
                            {
                                List<string> autores = new List<string>();

                                while (reader.Read())
                                {
                                    string autor = reader["Autor"].ToString();
                                    autores.Add(autor);
                                    Console.WriteLine(autor);
                                }
                                reader.Close();

                                var autorMenu = new ConsoleMenu().Configure(config => { config.WriteHeaderAction = () => Console.WriteLine($"Lista de Livros" + "\nEscolha uma opção:"); });
                                foreach (var autor in autores)
                                {
                                    autorMenu.Add(autor, () => {
                                        MostrarLivrosPorAutor(connection, autor, autorMenu);
                                        autorMenu.CloseMenu();
                                    });
                                }
                                autorMenu.Add("Voltar", ConsoleMenu.Close);


                                // Exibir o menu e aguardar a escolha do utilizador
                                autorMenu.Show();
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

        // Método privado para mostrar a lista de livros de um determinado autor
        private static void MostrarLivrosPorAutor(SqlConnection connection, string autorEscolhido, ConsoleMenu autorMenu)
        {
            // Consulta SQL para listar os livros do autor escolhido
            string livrosPorAutorQuery = "SELECT * FROM Livros WHERE Autor = @Autor";
            using (SqlCommand livrosPorAutorCommand = new SqlCommand(livrosPorAutorQuery, connection))
            {
                livrosPorAutorCommand.Parameters.AddWithValue("@Autor", autorEscolhido);

                using (SqlDataReader livrosPorAutorReader = livrosPorAutorCommand.ExecuteReader())
                {
                    // Se houver livros para o autor, mostra a lista
                    if (livrosPorAutorReader.HasRows)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"\nLista de Livros do Autor '{autorEscolhido}':");
                        Console.ResetColor();

                        while (livrosPorAutorReader.Read())
                        {
                            Console.WriteLine($"Título: {livrosPorAutorReader["Titulo"]}");
                            Console.WriteLine($"Autor: {livrosPorAutorReader["Autor"]}");
                            Console.WriteLine($"ISBN: {livrosPorAutorReader["ISBN"]}");
                            Console.WriteLine($"Preço: {livrosPorAutorReader["Preco"]}");
                            Console.WriteLine($"Taxa de IVA: {livrosPorAutorReader["TaxaIVA"]}");
                            Console.WriteLine($"Stock: {livrosPorAutorReader["Stock"]}\n");
                        }
                    }
                    else
                    {
                        // Se não houver livros para o autor, informa o utilizador
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Nenhum livro encontrado para o autor '{autorEscolhido}'.");
                    }
                }
            }
            Console.ReadKey();
            autorMenu.Show();
        }

        // Método para listar os utilizadores
        public static void listarUtilizadores()
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    // Consulta SQL para listar todos os utilizadores
                    string sqlQuery = "SELECT * FROM Users ORDER BY Name ASC";
                    using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // Se houver utilizadores, mostra a lista
                            if (reader.HasRows)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("Lista de Utilizadores:");
                                Console.ResetColor();

                                while (reader.Read())
                                {
                                    string role = " ";
                                    switch (reader["Role"].ToString())
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
                                    Console.WriteLine($"Utilizador {reader["Id"]}");
                                    Console.WriteLine($"Nome: {reader["Name"]}");
                                    Console.WriteLine($"Email: {reader["Email"]}");
                                    Console.WriteLine($"Cargo: {role}\n");
                                }
                            }
                            else
                            {
                                // Se não houver utilizadores, informa o utilizador
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Nenhum utilizador encontrado.");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro ao listar utilizadores: " + e.Message);
                }
                finally
                {
                    connection.Close();
                }
                Console.ReadKey();
                Console.Clear();
            }
        }

        public static void comprarLivros(string codigo)
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    // Verifica se o livro já existe pelo código
                    string checkIfExistsQuery = "SELECT * FROM Livros WHERE Codigo = @Codigo";
                    using (SqlCommand checkCommand = new SqlCommand(checkIfExistsQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Codigo", codigo);
                        using (SqlDataReader reader = checkCommand.ExecuteReader())
                        {
                            // Se o livro existe, informa o utilizador e pergunta quantos livros acrescentar ao stock
                            if (reader.Read())
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"Livro encontrado (Código: {codigo}):");
                                Console.WriteLine($"Título: {reader["Titulo"]}");
                                Console.WriteLine($"Autor: {reader["Autor"]}");
                                Console.WriteLine($"ISBN: {reader["ISBN"]}");
                                Console.WriteLine($"Género: {reader["Genero"]}");
                                Console.WriteLine($"Preço: {reader["Preco"]}");
                                Console.WriteLine($"Taxa de IVA: {reader["TaxaIVA"]}");
                                Console.WriteLine($"Stock Atual: {reader["Stock"]}");

                                Console.Write("\nQuantos livros deseja comprar? ");
                                int quantidade;
                                while (!int.TryParse(Console.ReadLine(), out quantidade) || quantidade < 0)
                                {
                                    Console.WriteLine("Por favor, insira um valor numérico válido para a quantidade.");
                                    Console.Write("Quantidade: ");
                                }

                                // Obtém o valor do stock antes de fechar o leitor
                                int stockAtual = (int)reader["Stock"];

                                // Fechar o leitor antes de executar o comando de atualização
                                reader.Close();

                                // Atualiza o stock do livro
                                int novoStock = stockAtual + quantidade;
                                AtualizarStockLivro(connection, codigo, novoStock);

                                Console.WriteLine($"{quantidade} livros adicionados ao stock com sucesso!");
                            }
                            else
                            {
                                //Fechar o leitor antes de executar o comando de inserção do livro
                                reader.Close();

                                // Se o livro não existe, pede informações para registar um novo livro
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"Livro com código {codigo} não encontrado.");

                                Console.WriteLine("Registar um novo livro:");

                                Console.Write("Título: ");
                                string titulo = Console.ReadLine();

                                Console.Write("Autor: ");
                                string autor = Console.ReadLine();

                                Console.Write("ISBN: ");
                                string ISBN = Console.ReadLine();

                                Console.Write("Género: ");
                                string genero = Console.ReadLine();

                                Console.Write("Preço: ");
                                decimal preco;
                                while (!decimal.TryParse(Console.ReadLine(), out preco) || preco < 0)
                                {
                                    Console.WriteLine("Por favor, insira um valor numérico válido para o preço.");
                                    Console.Write("Preço: ");
                                }

                                Console.Write("Taxa de IVA (6% ou 23%): ");
                                decimal taxaIVA;
                                while (!decimal.TryParse(Console.ReadLine(), out taxaIVA) || (taxaIVA != 6 && taxaIVA != 23))
                                {
                                    Console.WriteLine("Por favor, insira um valor numérico válido para a taxa de IVA (6 ou 23).");
                                    Console.Write("Taxa de IVA: ");
                                }

                                Console.Write("Stock: ");
                                int stock;
                                while (!int.TryParse(Console.ReadLine(), out stock) || stock < 0)
                                {
                                    Console.WriteLine("Por favor, insira um valor numérico válido para o stock.");
                                    Console.Write("Stock: ");
                                }

                                // Regista o novo livro
                                RegistarNovoLivro(connection, codigo, titulo, autor, ISBN, genero, preco, taxaIVA, stock);

                                Console.WriteLine("Livro registado com sucesso!");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro ao Comprar Livros: " + e.Message);
                }
                finally
                {
                    connection.Close();
                }
                Console.ReadKey();
                Console.Clear();
            }
        }

        // Método para registar um novo livro
        private static void RegistarNovoLivro(SqlConnection connection, string codigo, string titulo, string autor, string ISBN, string genero, decimal preco, decimal taxaIVA, int stock)
        {
            string insertQuery = "INSERT INTO Livros (Codigo, Titulo, Autor, ISBN, Genero, Preco, TaxaIVA, Stock) " +
                                 "VALUES (@Codigo, @Titulo, @Autor, @ISBN, @Genero, @Preco, @TaxaIVA, @Stock)";

            using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
            {
                insertCommand.Parameters.AddWithValue("@Codigo", codigo);
                insertCommand.Parameters.AddWithValue("@Titulo", titulo);
                insertCommand.Parameters.AddWithValue("@Autor", autor);
                insertCommand.Parameters.AddWithValue("@ISBN", ISBN);
                insertCommand.Parameters.AddWithValue("@Genero", genero);
                insertCommand.Parameters.AddWithValue("@Preco", preco);
                insertCommand.Parameters.AddWithValue("@TaxaIVA", taxaIVA);
                insertCommand.Parameters.AddWithValue("@Stock", stock);

                insertCommand.ExecuteNonQuery();
            }
        }

        // Método para atualizar o stock de um livro
        private static void AtualizarStockLivro(SqlConnection connection, string codigo, int novoStock)
        {
            string updateQuery = "UPDATE Livros SET Stock = @Stock WHERE Codigo = @Codigo";
            using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
            {
                updateCommand.Parameters.AddWithValue("@Codigo", codigo);
                updateCommand.Parameters.AddWithValue("@Stock", novoStock);

                updateCommand.ExecuteNonQuery();
            }
        }

        public static void venderLivros()
        {
            Console.OutputEncoding = Encoding.UTF8; // Configura a codificação para suportar o símbolo do euro

            // Informações fictícias da biblioteca
            string nomeBiblioteca = "Biblioteca IPCA CAMPUS";
            string moradaBiblioteca = "IPCA Famalicão, Vale São Cosme";

            using (SqlConnection connection = DBConnection.GetConnection())
            {
                try
                {
                    connection.Open();

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

                            Console.WriteLine("Livros disponíveis:");
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine(tableLivros.ToString());
                        }
                    }

                    decimal total = 0;
                    decimal desconto = 0;
                    decimal valorIVA = 0;

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nIntroduza os detalhes da venda (insira 0 como código do artigo para terminar):");

                    List<(string Codigo, string Titulo, int Quantidade)> vendas = new List<(string, string, int)>();

                    while (true)
                    {
                        Console.Write("Código do artigo: ");
                        string codigo = Console.ReadLine();

                        if (codigo == "0")
                            break;

                        Console.Write("Quantidade: ");
                        if (!int.TryParse(Console.ReadLine(), out int quantidade) || quantidade <= 0)
                        {
                            Console.WriteLine("Por favor, insira uma quantidade válida.");
                            continue;
                        }

                        // Verificar se o código existe e se há stock suficiente
                        if (!VerificarLivroExistente(connection, codigo, quantidade))
                        {
                            Console.WriteLine($"Erro: Livro com código {codigo} não encontrado ou stock insuficiente.");
                            continue;
                        }

                        // Obtém informações do livro para calcular o valor da venda
                        (decimal precoUnitario, decimal taxaIVA) = ObterDetalhesLivro(connection, codigo);

                        // Calcula o valor do item
                        decimal valorItem = quantidade * precoUnitario;

                        // Atualiza o total
                        total += valorItem;

                        // Calcula o valor do IVA para este item e adiciona ao valorIVA total
                        valorIVA += (valorItem * taxaIVA) / 100;

                        // Atualiza o stock na base de dados
                        AtualizarStock(connection, codigo, quantidade);

                        // Adiciona o item à lista
                        vendas.Add((codigo, ObterTituloLivro(connection, codigo), quantidade));

                        Console.WriteLine($"Item adicionado: Código: {codigo}, Quantidade: {quantidade}, Valor: {valorItem:C}");
                    }

                    // Aplica desconto se o total for superior a 50€
                    if (total > 50)
                    {
                        desconto = total * 0.1m;
                        total -= desconto;
                    }

                    // Cria um registro na tabela 'Vendas'
                    int idVenda;
                    using (SqlCommand insertVendaCommand = new SqlCommand("INSERT INTO Vendas (Total, Desconto, ValorIVA) VALUES (@Total, @Desconto, @ValorIVA); SELECT SCOPE_IDENTITY();", connection))
                    {
                        insertVendaCommand.Parameters.AddWithValue("@Total", total + valorIVA - desconto); // Atualiza o total considerando IVA e desconto
                        insertVendaCommand.Parameters.AddWithValue("@Desconto", desconto);
                        insertVendaCommand.Parameters.AddWithValue("@ValorIVA", valorIVA);

                        idVenda = Convert.ToInt32(insertVendaCommand.ExecuteScalar());
                    }

                    // Adiciona os detalhes de cada item vendido na tabela 'DetalhesVenda'
                    foreach (var venda in vendas)
                    {
                        using (SqlCommand insertDetalheCommand = new SqlCommand("INSERT INTO DetalhesVenda (IdVenda, CodigoLivro, Quantidade, Valor) VALUES (@IdVenda, @CodigoLivro, @Quantidade, @Valor);", connection))
                        {
                            insertDetalheCommand.Parameters.AddWithValue("@IdVenda", idVenda);
                            insertDetalheCommand.Parameters.AddWithValue("@CodigoLivro", venda.Codigo);
                            insertDetalheCommand.Parameters.AddWithValue("@Quantidade", venda.Quantidade);

                            // Recalcula o valor do item para garantir que está correto
                            (decimal precoUnitario, _) = ObterDetalhesLivro(connection, venda.Codigo);
                            decimal valorVendaDetalhe = venda.Quantidade * precoUnitario;

                            insertDetalheCommand.Parameters.AddWithValue("@Valor", valorVendaDetalhe);

                            insertDetalheCommand.ExecuteNonQuery();
                        }
                    }

                    Console.WriteLine($"Venda registada com sucesso. Fatura gerada para a venda com o ID: {idVenda}");
                    // Gera a fatura
                    ImprimirFatura(connection, nomeBiblioteca, moradaBiblioteca, vendas, total, desconto, valorIVA);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro ao Vender Livros: " + e.Message);
                }
                finally
                {
                    connection.Close();
                }
                Console.ReadKey();
                Console.Clear();
            }
        }

        private static string ObterTituloLivro(SqlConnection connection, string codigo)
        {
            string getTituloQuery = "SELECT Titulo FROM Livros WHERE Codigo = @Codigo";
            using (SqlCommand getTituloCommand = new SqlCommand(getTituloQuery, connection))
            {
                getTituloCommand.Parameters.AddWithValue("@Codigo", codigo);
                return getTituloCommand.ExecuteScalar() as string;
            }
        }


        private static void ImprimirFatura(SqlConnection connection, string nomeBiblioteca, string moradaBiblioteca, List<(string Codigo, string Titulo, int Quantidade)> vendas, decimal total, decimal desconto, decimal valorIVA)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Fatura:");
            Console.ResetColor();
            Console.WriteLine($"Biblioteca: {nomeBiblioteca}");
            Console.WriteLine($"Morada: {moradaBiblioteca}");
            Console.WriteLine($"Data/Hora: {DateTime.Now}");
            Console.WriteLine("--------------------------------------------------------");

            var table = new Table("Código", "Título", "Quantidade", "Preço Unitário", "Total", "Valor IVA");

            foreach (var venda in vendas)
            {
                (decimal precoUnitario, decimal taxaIVA) = ObterDetalhesLivro(connection, venda.Codigo);
                decimal valorVenda = venda.Quantidade * precoUnitario;
                decimal valorIVAItem = (valorVenda * taxaIVA) / 100;

                table.AddRow(venda.Codigo, venda.Titulo, venda.Quantidade, $"{precoUnitario:C}", $"{valorVenda:C}", $"{valorIVAItem:C}");
            }

            if (desconto > 0)
            {
                table.AddRow("Desconto", "", "", "", "", $"{desconto:C}");
                total -= desconto;
            }

            table.AddRow("Subtotal", "", "", "", "", $"{total:C}");
            table.AddRow("Valor do IVA", "", "", "", "", $"{valorIVA:C}");

            // Calcula o total depois do IVA
            decimal totalComIVA = total + valorIVA;
            table.AddRow("Total depois do IVA", "", "", "", "", $"{totalComIVA:C}");

            Console.WriteLine(table.ToString()); // ou Console.WriteLine(table.ToString()); para o formato padrão
            Console.WriteLine("--------------------------------------------------------");
        }

        // Verificar se o livro existe e se há stock suficiente
        private static bool VerificarLivroExistente(SqlConnection connection, string codigo, int quantidade)
        {
            string checkLivroQuery = "SELECT Stock FROM Livros WHERE Codigo = @Codigo";
            using (SqlCommand checkLivroCommand = new SqlCommand(checkLivroQuery, connection))
            {
                checkLivroCommand.Parameters.AddWithValue("@Codigo", codigo);

                using (SqlDataReader livroReader = checkLivroCommand.ExecuteReader())
                {
                    if (livroReader.Read())
                    {
                        int stockAtual = (int)livroReader["Stock"];
                        return stockAtual >= quantidade;
                    }
                }
            }

            return false;
        }

        // Obter detalhes do livro (preço unitário e taxa de IVA)
        private static (decimal PrecoUnitario, decimal TaxaIVA) ObterDetalhesLivro(SqlConnection connection, string codigo)
        {
            string getDetalhesQuery = "SELECT Preco, TaxaIVA FROM Livros WHERE Codigo = @Codigo";
            using (SqlCommand getDetalhesCommand = new SqlCommand(getDetalhesQuery, connection))
            {
                getDetalhesCommand.Parameters.AddWithValue("@Codigo", codigo);

                using (SqlDataReader detalhesReader = getDetalhesCommand.ExecuteReader())
                {
                    if (detalhesReader.Read())
                    {
                        decimal precoUnitario = (decimal)detalhesReader["Preco"];
                        decimal taxaIVA = (decimal)detalhesReader["TaxaIVA"];
                        return (precoUnitario, taxaIVA);
                    }
                }
            }

            // Valores padrão se não encontrar informações
            return (0, 0);
        }

        private static void AtualizarStock(SqlConnection connection, string codigoLivro, int quantidadeVendida)
        {
            // Obtenha o stock atual do livro
            string getStockQuery = "SELECT Stock FROM Livros WHERE Codigo = @Codigo";
            using (SqlCommand getStockCommand = new SqlCommand(getStockQuery, connection))
            {
                getStockCommand.Parameters.AddWithValue("@Codigo", codigoLivro);
                int stockAtual = (int)getStockCommand.ExecuteScalar();

                // Atualize o stock subtraindo a quantidade vendida
                int novoStock = stockAtual - quantidadeVendida;

                // Execute a atualização na base de dados
                string updateStockQuery = "UPDATE Livros SET Stock = @NovoStock WHERE Codigo = @Codigo";
                using (SqlCommand updateStockCommand = new SqlCommand(updateStockQuery, connection))
                {
                    updateStockCommand.Parameters.AddWithValue("@NovoStock", novoStock);
                    updateStockCommand.Parameters.AddWithValue("@Codigo", codigoLivro);
                    updateStockCommand.ExecuteNonQuery();
                }
            }
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

        public static void consultarReceita()
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    // Consultar o número total de livros vendidos
                    string queryTotalLivrosVendidos = "SELECT CodigoLivro, SUM(Quantidade) AS TotalLivrosVendidos, COUNT(DISTINCT IdVenda) AS TotalVendas " +
                                                      "FROM DetalhesVenda " +
                                                      "GROUP BY CodigoLivro";
                    using (SqlCommand totalLivrosVendidosCommand = new SqlCommand(queryTotalLivrosVendidos, connection))
                    {
                        using (SqlDataReader totalLivrosVendidosReader = totalLivrosVendidosCommand.ExecuteReader())
                        {
                            Console.WriteLine("Total de livros vendidos e número de vendas por livro:");
                            var tableTotalLivrosVendidos = new Table("Código Livro", "Total Livros Vendidos", "Total Vendas");

                            Console.ForegroundColor= ConsoleColor.White;

                            while (totalLivrosVendidosReader.Read())
                            {
                                tableTotalLivrosVendidos.AddRow(
                                    totalLivrosVendidosReader["CodigoLivro"].ToString(),
                                    totalLivrosVendidosReader["TotalLivrosVendidos"].ToString(),
                                    totalLivrosVendidosReader["TotalVendas"].ToString()
                                );
                            }

                            Console.WriteLine(tableTotalLivrosVendidos.ToString());
                        }
                    }

                    // Consultar a receita total
                    string queryReceitaTotal = "SELECT SUM(Valor) AS ReceitaTotal FROM DetalhesVenda";
                    using (SqlCommand receitaTotalCommand = new SqlCommand(queryReceitaTotal, connection))
                    {
                        decimal receitaTotal = Convert.ToDecimal(receitaTotalCommand.ExecuteScalar());
                        Console.WriteLine($"Receita total: {receitaTotal:C}");
                    }

                    // Consultar a receita por livro
                    string queryReceitaPorLivro = "SELECT CodigoLivro, SUM(Valor) AS ReceitaPorLivro " +
                                                  "FROM DetalhesVenda " +
                                                  "GROUP BY CodigoLivro";
                    using (SqlCommand receitaPorLivroCommand = new SqlCommand(queryReceitaPorLivro, connection))
                    {
                        using (SqlDataReader receitaPorLivroReader = receitaPorLivroCommand.ExecuteReader())
                        {
                            Console.WriteLine("Receita por livro:");
                            var tableReceitaPorLivro = new Table("Código Livro", "Receita");

                            while (receitaPorLivroReader.Read())
                            {
                                tableReceitaPorLivro.AddRow(
                                    receitaPorLivroReader["CodigoLivro"].ToString(),
                                    $"{receitaPorLivroReader["ReceitaPorLivro"]:C}"
                                );
                            }

                            Console.WriteLine(tableReceitaPorLivro.ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro ao consultar a receita: " + e.Message);
                }
                finally
                {
                    connection.Close();
                }
                Console.ReadKey();
            }
        }

        public static void criarUtilizador()
        {
            Console.WriteLine("Criar Novo Utilizador");

            Console.Write("Nome: ");
            string nome = Console.ReadLine();

            Console.Write("Email: ");
            string email = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();

            Console.Write("Role (G: Gerente, R: Repositor; C: Caixa): ");
            string role = Console.ReadLine();

            // Outras informações podem ser solicitadas conforme necessário

            using (SqlConnection connection = DBConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    // Verificar se o email já está em uso
                    if (VerificarEmailExistente(connection, email))
                    {
                        Console.WriteLine("Erro: Este email já está em uso. Escolha outro.");
                        return;
                    }

                    // Criar um novo utilizador na base de dados
                    string queryInserirUtilizador = "INSERT INTO users (Name, Email, Password, Role) VALUES (@Nome, @Email, @Password, @Role)";
                    using (SqlCommand inserirUtilizadorCommand = new SqlCommand(queryInserirUtilizador, connection))
                    {
                        inserirUtilizadorCommand.Parameters.AddWithValue("@Nome", nome);
                        inserirUtilizadorCommand.Parameters.AddWithValue("@Email", email);
                        inserirUtilizadorCommand.Parameters.AddWithValue("@Password", password);
                        inserirUtilizadorCommand.Parameters.AddWithValue("@Role", role);

                        int linhasAfetadas = inserirUtilizadorCommand.ExecuteNonQuery();

                        if (linhasAfetadas > 0)
                        {
                            Console.WriteLine("Utilizador criado com sucesso!");
                        }
                        else
                        {
                            Console.WriteLine("Erro ao criar utilizador. Tente novamente.");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro ao criar utilizador: " + e.Message);
                }
                finally
                {
                    connection.Close();
                }
                Console.ReadKey();
            }
        }

        private static bool VerificarEmailExistente(SqlConnection connection, string email)
        {
            string queryVerificarEmail = "SELECT COUNT(*) FROM users WHERE Email = @Email";
            using (SqlCommand verificarEmailCommand = new SqlCommand(queryVerificarEmail, connection))
            {
                verificarEmailCommand.Parameters.AddWithValue("@Email", email);

                int count = Convert.ToInt32(verificarEmailCommand.ExecuteScalar());

                return count > 0;
            }
        }

        public static void eliminarUtilizador(int currentUserId)
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    var menu = new ConsoleMenu()
                         .Configure(config => { config.WriteHeaderAction = () => Console.WriteLine($"Lista de Utilizadores" + "\nEscolha uma opção:"); })
                        .Add("Voltar", ConsoleMenu.Close);

                    // Obter todos os utilizadores, exceto o utilizador atual
                    string queryObterUtilizadores = "SELECT Id, Name FROM Users WHERE Id != @CurrentUserId";
                    using (SqlCommand obterUtilizadoresCommand = new SqlCommand(queryObterUtilizadores, connection))
                    {
                        obterUtilizadoresCommand.Parameters.AddWithValue("@CurrentUserId", currentUserId);

                        using (SqlDataReader utilizadoresReader = obterUtilizadoresCommand.ExecuteReader())
                        {
                            Console.WriteLine("Escolha o utilizador a eliminar:");

                            Dictionary<int, string> utilizadores = new Dictionary<int, string>();

                            while (utilizadoresReader.Read())
                            {
                                int userId = Convert.ToInt32(utilizadoresReader["Id"]);
                                string userName = utilizadoresReader["Name"].ToString();

                                utilizadores.Add(userId, userName);

                                menu.Add($"{userId}. {userName}", () =>
                                {
                                    string utilizadorEscolhido = utilizadores[userId];

                                    Console.Write($"Tem a certeza que deseja eliminar o utilizador {utilizadorEscolhido}? (S/N): ");
                                    string resposta = Console.ReadLine().Trim().ToUpper();

                                    if (resposta == "S")
                                    {
                                        // Eliminar o utilizador
                                        EliminarUtilizador(connection, userId);
                                        Console.WriteLine($"Utilizador {utilizadorEscolhido} eliminado com sucesso!");
                                    }
                                    else
                                    {
                                        Console.WriteLine("Operação cancelada.");
                                    }
                                });
                            }
                        }
                    }

                    menu.Show();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro ao eliminar utilizador: " + e.Message);
                }
                finally
                {
                    connection.Close();
                }
                Console.ReadKey();
            }
        }

        private static void EliminarUtilizador(SqlConnection connection, int userId)
        {
            string queryEliminarUtilizador = "DELETE FROM Users WHERE Id = @UserId";
            using (SqlCommand eliminarUtilizadorCommand = new SqlCommand(queryEliminarUtilizador, connection))
            {
                eliminarUtilizadorCommand.Parameters.AddWithValue("@UserId", userId);
                eliminarUtilizadorCommand.ExecuteNonQuery();
            }
        }

        public static void listarUtilizadoresTabela()
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    // Obter todos os utilizadores
                    string queryObterUtilizadores = "SELECT Id, Name, Email, Role FROM Users";
                    using (SqlCommand obterUtilizadoresCommand = new SqlCommand(queryObterUtilizadores, connection))
                    {
                        using (SqlDataReader utilizadoresReader = obterUtilizadoresCommand.ExecuteReader())
                        {
                            Console.WriteLine("Lista de Utilizadores:");

                            var table = new Table("ID", "Nome", "Email", "Role");

                            while (utilizadoresReader.Read())
                            {
                                int userId = Convert.ToInt32(utilizadoresReader["Id"]);
                                string userName = utilizadoresReader["Name"].ToString();
                                string userEmail = utilizadoresReader["Email"].ToString();
                                string userRole = utilizadoresReader["Role"].ToString();

                                switch(userRole)
                                {
                                    case "G":
                                        userRole = "Gerente";
                                        break;
                                    case "C":
                                        userRole = "Caixa";
                                        break;
                                    case "R":
                                        userRole = "Repositor";
                                        break;
                                }

                                table.AddRow(userId.ToString(), userName, userEmail, userRole);
                            }

                            Console.WriteLine(table.ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro ao listar utilizadores: " + e.Message);
                }
                finally
                {
                    connection.Close();
                }
                Console.ReadKey();
            }
        }


    }

}



