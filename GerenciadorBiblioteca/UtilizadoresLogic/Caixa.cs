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
    public static class Caixa
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

        public static void realizarEmprestimo()
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    // Obter livros disponíveis
                    string queryLivrosDisponiveis = "SELECT Codigo, Titulo, Autor, Stock FROM Livros WHERE Stock > 0";
                    using (SqlCommand livrosDisponiveisCommand = new SqlCommand(queryLivrosDisponiveis, connection))
                    {
                        using (SqlDataReader livrosReader = livrosDisponiveisCommand.ExecuteReader())
                        {
                            Console.WriteLine("Escolha o livro a ser emprestado:");

                            var menuLivros = new ConsoleMenu()
                                .Configure(config => { config.WriteHeaderAction = () => Console.WriteLine($"Lista de Livros" + "\nEscolha uma opção:"); });

                            Dictionary<string, string> livrosDisponiveis = new Dictionary<string, string>();

                            while (livrosReader.Read())
                            {
                                string codigoLivro = livrosReader["Codigo"].ToString();
                                string tituloLivro = livrosReader["Titulo"].ToString();
                                int stockLivro = Convert.ToInt32(livrosReader["Stock"]);

                                livrosDisponiveis.Add(codigoLivro, $"{tituloLivro} (Stock: {stockLivro})");

                                menuLivros.Add($"{codigoLivro}. {tituloLivro} (Stock: {stockLivro})", () => EmprestarLivro(codigoLivro, connection));


                            }

                            livrosReader.Close();

                            menuLivros.Add("Cancelar Operação", ConsoleMenu.Close);
                            menuLivros.Show();


                        }


                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro ao realizar empréstimo: " + e.Message);
                }
                Console.ReadKey();
            }
        }

        private static void EmprestarLivro(string codigoLivro, SqlConnection connection)
        {
            Console.Write("Nome da pessoa: ");
            string nomePessoa = Console.ReadLine();

            Console.Write("Número da pessoa: ");
            string numeroPessoa = Console.ReadLine();

            Console.Write("Email da pessoa: ");
            string emailPessoa = Console.ReadLine();

            DateTime dataEmprestimo = DateTime.Now.Date;

            SqlTransaction transaction = null;

            try
            {
                // Iniciar a transação
                transaction = connection.BeginTransaction();

                // Registrar o empréstimo na tabela Emprestimos
                string queryRegistrarEmprestimo = "INSERT INTO Emprestimos (CodigoLivro, NomePessoa, NumeroPessoa, EmailPessoa, DataEmprestimo, Devolvido) " +
                                                  "VALUES (@CodigoLivro, @NomePessoa, @NumeroPessoa, @EmailPessoa, @DataEmprestimo, 0)";
                using (SqlCommand registrarEmprestimoCommand = new SqlCommand(queryRegistrarEmprestimo, connection, transaction))
                {
                    registrarEmprestimoCommand.Parameters.AddWithValue("@CodigoLivro", codigoLivro);
                    registrarEmprestimoCommand.Parameters.AddWithValue("@NomePessoa", nomePessoa);
                    registrarEmprestimoCommand.Parameters.AddWithValue("@NumeroPessoa", numeroPessoa);
                    registrarEmprestimoCommand.Parameters.AddWithValue("@EmailPessoa", emailPessoa);
                    registrarEmprestimoCommand.Parameters.AddWithValue("@DataEmprestimo", dataEmprestimo);

                    registrarEmprestimoCommand.ExecuteNonQuery();

                    Console.WriteLine("Empréstimo realizado com sucesso!");

                    // Atualizar o estoque
                    AtualizarEstoqueLivro(connection, transaction, codigoLivro, 1, "-");
                }

                // Commit na transação se tudo ocorreu sem problemas
                transaction.Commit();
            }
            catch (Exception e)
            {
                // Em caso de erro, fazer rollback da transação
                Console.WriteLine("Erro ao realizar empréstimo: " + e.Message);
                transaction?.Rollback();
            }
            finally
            {
                transaction?.Dispose();
            }

            Console.ReadKey();
        }

        private static void AtualizarEstoqueLivro(SqlConnection connection, SqlTransaction transaction, string codigoLivro, int stock, string operacao)
        {
            // Atualizar o estoque após o empréstimo
            string queryAtualizarEstoque = "UPDATE Livros SET Stock = Stock " + operacao + " @Stock WHERE Codigo = @CodigoLivro";

            using (SqlCommand atualizarEstoqueCommand = new SqlCommand(queryAtualizarEstoque, connection, transaction))
            {
                atualizarEstoqueCommand.Parameters.AddWithValue("@Stock", stock);
                atualizarEstoqueCommand.Parameters.AddWithValue("@CodigoLivro", codigoLivro);
                atualizarEstoqueCommand.ExecuteNonQuery();
            }
        }

        public static void realizarDevolucao()
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    // Obter empréstimos não devolvidos
                    string queryEmprestimos = "SELECT IdEmprestimo, CodigoLivro, NomePessoa, DataEmprestimo FROM Emprestimos WHERE Devolvido = 0";
                    using (SqlCommand emprestimosCommand = new SqlCommand(queryEmprestimos, connection))
                    {
                        using (SqlDataReader emprestimosReader = emprestimosCommand.ExecuteReader())
                        {
                            Console.WriteLine("Escolha o empréstimo a ser devolvido:");

                            var menuEmprestimos = new ConsoleMenu()
                                .Configure(config => { config.WriteHeaderAction = () => Console.WriteLine($"Lista de Empréstimos" + "\nEscolha uma opção:"); });

                            while (emprestimosReader.Read())
                            {
                                int idEmprestimo = Convert.ToInt32(emprestimosReader["IdEmprestimo"]);
                                string codigoLivro = emprestimosReader["CodigoLivro"].ToString();
                                string nomePessoa = emprestimosReader["NomePessoa"].ToString();
                                DateTime dataEmprestimo = Convert.ToDateTime(emprestimosReader["DataEmprestimo"]);

                                menuEmprestimos.Add($"{idEmprestimo}. Livro: {codigoLivro}, Pessoa: {nomePessoa}, Data: {dataEmprestimo.ToShortDateString()}", () => DevolverLivro(idEmprestimo, codigoLivro, connection));
                            }

                            emprestimosReader.Close();

                            menuEmprestimos.Add("Cancelar Operação", ConsoleMenu.Close);
                            menuEmprestimos.Show();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro ao realizar devolução: " + e.Message);
                }
                Console.ReadKey();
            }
        }

        private static void DevolverLivro(int idEmprestimo, string codigoLivro, SqlConnection connection)
        {
            Console.WriteLine($"Tem certeza que deseja devolver o livro com o ID de empréstimo {idEmprestimo}? (S/N)");
            string resposta = Console.ReadLine();

            if (resposta.Equals("S", StringComparison.OrdinalIgnoreCase))
            {
                SqlTransaction transaction = null;

                try
                {
                    // Iniciar a transação
                    transaction = connection.BeginTransaction();

                    // Atualizar o estoque
                    AtualizarEstoqueLivro(connection, transaction, codigoLivro, 1, "+");

                    // Marcar empréstimo como devolvido
                    string queryDevolverLivro = "UPDATE Emprestimos SET Devolvido = 1 WHERE IdEmprestimo = @IdEmprestimo";
                    using (SqlCommand devolverLivroCommand = new SqlCommand(queryDevolverLivro, connection, transaction))
                    {
                        devolverLivroCommand.Parameters.AddWithValue("@IdEmprestimo", idEmprestimo);
                        devolverLivroCommand.ExecuteNonQuery();

                        Console.WriteLine("Devolução realizada com sucesso!");
                    }

                    // Commit na transação se tudo ocorreu sem problemas
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    // Em caso de erro, fazer rollback da transação
                    Console.WriteLine("Erro ao realizar devolução: " + e.Message);
                    transaction?.Rollback();
                }
                finally
                {
                    transaction?.Dispose();
                }
            }
            else
            {
                Console.WriteLine("Operação de devolução cancelada.");
            }

            Console.ReadKey();
        }

        //Lógica para realizar uma venda

        public static void realizarVenda()
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

        //Lógica para consultar stock

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
