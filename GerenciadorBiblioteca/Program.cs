using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static GerenciadorBiblioteca.login;

namespace GerenciadorBiblioteca
{
    public static class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.Clear();

            // Exibir cabeçalho
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Bem-vindo à Biblioteca\n");

            // Loop para o login
            login.UserData user = null;
            do
            {
                Console.ForegroundColor = ConsoleColor.White;

                Console.Write("Nome de Utilizador: ");
                string name = Console.ReadLine();

                Console.Write("Password: ");
                string password = GetHiddenInput();

                // Chama o método ValidateLogin para obter os dados do utilizador
                user = login.ValidateLogin(name, password);

                if (user == null)
                {
                    // Limpa a tela e imprime uma mensagem de erro em vermelho
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nLogin falhou. Por favor, verifique suas credenciais.\n");
                }

            } while (user == null);

            // Limpa a tela após o login bem-sucedido
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nLogin bem-sucedido!\n");
            Console.ReadKey();

            // Chama o método ShowMenu com base no papel do utilizador
            Menu.ShowMenu(user.Role, user.Name, user.Id);
        }

        // Função para obter entrada de password oculta
        static string GetHiddenInput()
        {
            string password = "";
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password.Substring(0, (password.Length - 1));
                    Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);

            Console.WriteLine(); // Pular linha após a entrada da senha
            return password;
        }
        
    }
    
}
