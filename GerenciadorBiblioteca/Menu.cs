using ConsoleTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerenciadorBiblioteca
{
    public static class Menu
    {

        public static class UserInfo
        {
            public static int Id { get; set; }
        }

        public static void ShowMenu(string role, string name, int id)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;

            UserInfo.Id = id;

            // Criar menu baseado no papel do utilizador
            var menu = new ConsoleMenu()
                .Add("Sair", ConsoleMenu.Close)
                .Configure(config => { config.WriteHeaderAction = () => Console.WriteLine($"Bem-vindo, {role} {name}!" + "\nEscolha uma opção:"); });


            switch (role)
            {
                case "Gerente":
                    ConfigureGerenteOptions(menu);
                    break;
                case "Caixa":
                    ConfigureCaixaOptions(menu);
                    break;
                case "Repositor":
                    ConfigureRepositorOptions(menu);
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Papel de utilizador não reconhecido.");
                    Console.ResetColor();
                    break;
            }

            // Adicionar a opção de sair em qualquer menu
            menu.Add("Sair", ConsoleMenu.Close);

            // Mostrar o menu
            menu.Show();
        }

        private static bool GetUserConfirmation()
        {
            Console.Write("Quer voltar atrás? (S/N): ");
            string input = Console.ReadLine().ToUpper();
            return input == "S";
        }

        private static void ConfigureGerenteOptions(ConsoleMenu menu)
        {
            menu.Add("Registar Livro", () =>
            {
                // Lógica para registar um novo livro
                Console.Clear();
                Console.WriteLine("Registar um novo livro:");


                if (GetUserConfirmation())
                {
                    Console.Clear();
                    return;
                }
                else
                {
                    Console.Clear();
                }

                Console.ForegroundColor = ConsoleColor.Green;

                Console.Write("Código: ");
                string registarLivroCodigo = Console.ReadLine();

                Console.Write("Título: ");
                string registarLivroTitulo = Console.ReadLine();

                Console.Write("Autor: ");
                string registarLivroAutor = Console.ReadLine();

                Console.Write("ISBN: ");
                string registarLivroISBN = Console.ReadLine();

                Console.Write("Género: ");
                string registarLivroGenero = Console.ReadLine();

                Console.Write("Preço: ");
                decimal registarLivroPreco;
                while (!decimal.TryParse(Console.ReadLine(), out registarLivroPreco))
                {
                    Console.WriteLine("Por favor, insira um valor numérico para o preço.");
                    Console.Write("Preço: ");
                }

                Console.Write("Taxa de IVA (6% ou 23%): ");
                decimal registarLivroTaxaIVA;
                while (!decimal.TryParse(Console.ReadLine(), out registarLivroTaxaIVA))
                {
                    Console.WriteLine("Por favor, insira um valor numérico para a taxa de IVA.");
                    Console.Write("Taxa de IVA: ");
                }

                Console.Write("Stock: ");
                int registarLivroStock;
                while (!int.TryParse(Console.ReadLine(), out registarLivroStock))
                {
                    Console.WriteLine("Por favor, insira um valor numérico para o stock.");
                    Console.Write("Stock: ");
                }

                // Chamada à função para registar o livro
                UtilizadoresLogic.Gerente.registarLivro(registarLivroCodigo, registarLivroTitulo, registarLivroAutor, registarLivroISBN, registarLivroGenero, registarLivroPreco, registarLivroTaxaIVA, registarLivroStock);
            });
            menu.Add("Atualizar Livro", () => {
                // Lógica para atualizar um livro
                Console.Clear();

                Console.ForegroundColor = ConsoleColor.Green;


                // Chamada à função para registar o livro
                UtilizadoresLogic.Gerente.atualizarLivro();
            });


            menu.Add("Consultar Informação do Livro por Código", () =>
            {
                // Lógica para consultar informações do livro por código
                Console.Clear();

                Console.ForegroundColor = ConsoleColor.Green;

                // Chamada à função para consultar o livro
                UtilizadoresLogic.Gerente.consultarLivroCodigo();

            });
            menu.Add("Listar Livros pelo Género", () =>
            {
                // Lógica para listar livros por género
                Console.Clear();

                UtilizadoresLogic.Gerente.listarLivrosGenero();

            });
            menu.Add("Listar Livros pelo Autor", () =>
            {
                Console.Clear();
                // Lógica para listar livros por autor

                UtilizadoresLogic.Gerente.listarLivrosAutor();

            });
            menu.Add("Listar Utilizadores", () =>
            {
                // Lógica para listar utilizadores
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Listar Utilizadores");


                if (GetUserConfirmation())
                {
                    Console.Clear();
                    return;
                }
                else
                {
                    Console.Clear();
                }

                UtilizadoresLogic.Gerente.listarUtilizadores();

            });
            menu.Add("Comprar Livros (Acrescentar ao Stock)", () =>
            {
                //Lógica para comprar livros
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Comprar Livros: ");

                if (GetUserConfirmation())
                {
                    Console.Clear();
                    return;
                }
                else
                {
                    Console.Clear();
                }

                Console.ForegroundColor = ConsoleColor.Green;

                Console.Write("Código do Livro: ");
                string codigoComprarLivro = Console.ReadLine();

                UtilizadoresLogic.Gerente.comprarLivros(codigoComprarLivro);

            });
            menu.Add("Vender Livros (Reduzir ao Stock)", () =>
            {
                //Lógica para vender livros
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Vender Livros: ");

                if (GetUserConfirmation())
                {
                    Console.Clear();
                    return;
                }
                else
                {
                    Console.Clear();
                }

                Console.ForegroundColor = ConsoleColor.Green;

                UtilizadoresLogic.Gerente.venderLivros();


            });
            menu.Add("Consultar Stock", () =>
            {
                Console.Clear();
                // Lógica para consultar Stock

                UtilizadoresLogic.Gerente.consultarStock();
            });
            menu.Add("Consultar Total de Livros Vendidos e Receita", () =>
            {
                Console.Clear();
                //Lógica para consultar Total de Livros Vendidos e Receita

                UtilizadoresLogic.Gerente.consultarReceita();

            });
            menu.Add("Listar Utilizadores", () =>
            {
                Console.Clear();

                UtilizadoresLogic.Gerente.listarUtilizadoresTabela();
            });
            menu.Add("Criar Novo Utilizador", () =>
            {
                Console.Clear();
                //Lógica para criar um novo utilizador

                UtilizadoresLogic.Gerente.criarUtilizador();

            });
            menu.Add("Eliminar Utilizador", () =>
            {
                Console.Clear();
            //Logica para Eliminar Utilizadores

            UtilizadoresLogic.Gerente.eliminarUtilizador(UserInfo.Id);

            });
        }

        private static void ConfigureCaixaOptions(ConsoleMenu menu)
        {
            menu.Add("Ver Livros Disponíveis", () =>
            {
                Console.Clear();

                //Lógica para mostrar os Livros Disponiveis
                UtilizadoresLogic.Caixa.listarLivrosDisponiveis();
            });
            menu.Add("Emprestar Livro", () =>
            {
                Console.Clear();
                
                //Lógica para emprestrar livros
                UtilizadoresLogic.Caixa.realizarEmprestimo();

            });
            menu.Add("Devolver Livro", () =>
            {
                Console.Clear();

                UtilizadoresLogic.Caixa.realizarDevolucao();
            });
            menu.Add("Vender Livros (Reduzir ao Stock)", () =>
            {
                Console.Clear();

                UtilizadoresLogic.Caixa.realizarVenda();
            });
            menu.Add("Consultar Stock", () =>
            {
                Console.Clear();

                UtilizadoresLogic.Caixa.consultarStock();
            });
        }

        private static void ConfigureRepositorOptions(ConsoleMenu menu)
        {
            menu.Add("Ver Livros Disponíveis", () =>
            {
                Console.Clear();

                UtilizadoresLogic.Repositor.listarLivrosDisponiveis();
            });
            menu.Add("Adicionar Livro", () =>
            {
                UtilizadoresLogic.Repositor.adicionarLivro();
            });
            menu.Add("Consultar Stock", () =>
            {
                UtilizadoresLogic.Repositor.consultarStock();
            });
        }
    }
}
