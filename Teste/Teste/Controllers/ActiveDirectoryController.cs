using System;
using System.DirectoryServices;
using Microsoft.AspNetCore.Mvc;
using Teste.Models;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Extensions.Configuration;


[Route("api/ad")]
[ApiController]
public class ActiveDirectoryController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly string _ldapPath;
    private readonly string _adminUser;
    private readonly string _adminPassword;


        //public ActiveDirectoryController(IConfiguration config)
        //{
           // _config = config;
            //_ldapPath = _config["LDAP:Path"];
        //}

        //public bool IsUserInGroup(string groupName)
        //{
           // try
            //{
             //   // Obtém o nome do usuário logado
             //   string currentUser = WindowsIdentity.GetCurrent().Name;

                // Cria uma conexão LDAP usando o nome de usuário e a senha do Windows
             //   DirectorySearcher searcher = new DirectorySearcher();
             //   searcher.Filter = $"(&(objectClass=user)(sAMAccountName={currentUser}))";
             //   searcher.PropertiesToLoad.Add("memberOf");

                // Executa a pesquisa no AD
               // SearchResult result = searcher.FindOne();

               // if (result != null)
               // {
                    // Obtém os grupos do usuário
                //    var groups = result.Properties["memberOf"];
                //    foreach (var group in groups)
                  //  {
                    //    if (group.ToString().ToLower().Contains(groupName.ToLower())) // Verifica se o usuário é membro do grupo
                     //   {
                      //      return true; // O usuário é membro do grupo
                     //   }
                   // }
               // }

              //  return false; // O usuário não é membro do grupo
            //}
            //catch (Exception ex)
           // {
            //    Console.WriteLine($"Erro ao verificar grupos: {ex.Message}");
            //    return false;
            //}
        //}
    


    public ActiveDirectoryController(IConfiguration config)
    {
        _config = config;
        _ldapPath = _config["LDAP:Path"];
        _adminUser = _config["LDAP:AdminUser"];
        _adminPassword = _config["LDAP:AdminPassword"];
    }

    [HttpPost("create-user")]
    public IActionResult CreateUser([FromBody] UserDto user)
    {
        try
        {
            string ldapPath = $"LDAP://{user.OrganizationalUnit},DC=prefeitura,DC=local";



            using (DirectoryEntry ouEntry = new DirectoryEntry(ldapPath, _adminUser, _adminPassword))
            {
                DirectoryEntries users = ouEntry.Children;
                DirectoryEntry newUser = users.Add($"CN={user.Name} {user.Surname}", "user");


                newUser.Properties["sAMAccountName"].Value = user.Login;
                newUser.Properties["givenName"].Value = user.Name;
                newUser.Properties["sn"].Value = user.Surname;
                newUser.Properties["department"].Value = user.Departamento;
                newUser.Properties["title"].Value = user.Cargo;
                newUser.Properties["telephoneNumber"].Value = user.Telefone;
                newUser.Properties["displayName"].Value = user.NomeCompleto;
                newUser.Properties["userPrincipalName"].Value = $"{user.Login}@prefeitura.local";
                newUser.Properties["CPF"].Value = user.CPF; // Atributo personalizado para CPF

                ////
                int userAccountControl = 0x0200; // Criar usuário ativado

                //if (user.AlterarSenha) userAccountControl |= 0x800000; //Não está funcionando
                if (user.SenhaNuncaExpira) userAccountControl |= 0x10000; // Está funcionando
                if (user.ContaDesabilitada) userAccountControl |= 0x0002; //Está funcionando
                newUser.Properties["userAccountControl"].Value = userAccountControl;

//////
                var Password = "Betim1234";

                // Agora definir a senha
                // Convertendo senha para formato esperado pelo AD
                string password = "Betim1234";
                byte[] passwordBytes = Encoding.Unicode.GetBytes($"\"{password}\"");

                // Definir diretamente no atributo unicodePwd
                newUser.Properties["unicodePwd"].Value = passwordBytes;
                newUser.CommitChanges();

                ////
                if (user.DataExpiracao != 0) // Não está funcionando
                {
                    //Função para definir a data em que a conta do usuário expira
                    DateTime expirationDate = DateTime.FromFileTimeUtc(user.DataExpiracao);
                    newUser.Properties["accountExpires"].Value = expirationDate.ToFileTimeUtc(); 

                }
                else
                {
                    newUser.Properties["accountExpires"].Value = 0; // Nunca expira
                }

/////
                // Adicionar usuário aos grupos
                foreach (string grupoDN in user.Grupos) // Agora já vem o caminho completo do grupo
                {
                    using (DirectoryEntry groupEntry = new DirectoryEntry($"LDAP://{grupoDN}", _adminUser, _adminPassword))
                    {
                        groupEntry.Properties["member"].Add(newUser.Properties["distinguishedName"].Value);
                        groupEntry.CommitChanges();
                    }
                }



                return Ok(new { message = "Usuário criado na OU correta e adicionado aos grupos com sucesso!" });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao criar usuário: {ex.Message} \nStackTrace: {ex.StackTrace}");
            
            return BadRequest(new { error = $"Erro ao criar usuário: {ex.Message}", stackTrace = ex.StackTrace });
        }

    }
    ///
    [DllImport("Netapi32.dll", SetLastError = true)]
    public static extern int NetUserSetInfo(
    string servername,
    string username,
    int level,
    ref USER_INFO_1003 buf,
    out int parm_err);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct USER_INFO_1003
    {
        public string usri1003_password;
    }

    // Método para definir senha
    public static void ChangeUserPassword(string username, string newPassword)
    {
        USER_INFO_1003 userInfo = new USER_INFO_1003
        {
            usri1003_password = newPassword
        };

        int result = NetUserSetInfo(null, username, 1003, ref userInfo, out int parm_err);
        if (result != 0)
        {
            throw new Exception($"Erro ao definir senha: {result}");
        }
    }

    ///

    [HttpGet("organizational-units")]
    public IActionResult GetOrganizationalUnits()
    {
        try
        {
            string ldapPath = "LDAP://DC=prefeitura,DC=local"; // Ajuste conforme necessário
            using (DirectoryEntry entry = new DirectoryEntry(ldapPath))
            using (DirectorySearcher searcher = new DirectorySearcher(entry))
            {
                searcher.Filter = "(objectClass=organizationalUnit)";
                searcher.PropertiesToLoad.Add("ou");
                searcher.PropertiesToLoad.Add("distinguishedName");

                List<string> ouList = new List<string>();

                foreach (SearchResult result in searcher.FindAll())
                {
                    if (result.Properties.Contains("ou") && result.Properties.Contains("distinguishedName"))
                    {
                        string ouName = result.Properties["ou"][0].ToString();
                        string dn = result.Properties["distinguishedName"][0].ToString();
                        ouList.Add($"{FormatDnToHierarchy(dn)}"); // Converte para hierarquia legível
                    }
                }

                if (ouList.Count == 0)
                {
                    return NotFound(new { error = "Nenhuma OU encontrada." });
                }

                return Ok(ouList);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Erro ao buscar OUs", message = ex.Message });
        }
    }

    // Função para converter Distinguished Name para um formato legível
    private string FormatDnToHierarchy(string distinguishedName)
    {
        var parts = distinguishedName.Split(',');
        List<string> hierarchy = new List<string>();

        foreach (var part in parts)
        {
            if (part.StartsWith("OU="))
            {
                hierarchy.Insert(0, part.Replace("OU=", "")); // Insere no início para ordem correta
            }
        }

        return string.Join(" > ", hierarchy);
    }



    [HttpGet("list-groups")]
    public IActionResult ListGroups()
    {
        try
        {
            List<object> groups = new List<object>(); // Lista de objetos para armazenar nome e caminho

            using (DirectoryEntry entry = new DirectoryEntry(_ldapPath, _adminUser, _adminPassword))
            using (DirectorySearcher searcher = new DirectorySearcher(entry))
            {
                searcher.Filter = "(objectClass=group)";
                searcher.PropertiesToLoad.Add("cn");  // Nome do grupo
                searcher.PropertiesToLoad.Add("distinguishedName"); // Caminho completo do grupo

                foreach (SearchResult result in searcher.FindAll())
                {
                    string groupName = result.Properties["cn"][0].ToString();
                    string dn = result.Properties.Contains("distinguishedName") ? result.Properties["distinguishedName"][0].ToString() : "N/A";

                    groups.Add(new { Nome = groupName, Caminho = dn }); // Adiciona um objeto com Nome e Caminho
                }
            }

            return Ok(groups); // Retorna uma lista de objetos JSON
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao listar grupos: {ex.Message}");
        }
    }

}

