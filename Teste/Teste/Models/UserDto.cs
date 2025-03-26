namespace Teste.Models
{
    public class UserDto
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string NomeCompleto { get; set; }
        public string Departamento { get; set; }
        public string Login { get; set; }
        public string Telefone { get; set; }
        public string CPF { get; set; }
        public string Cargo { get; set; }
        public string OrganizationalUnit { get; set; }
        public List<string> Grupos { get; set; } = new List<string>();
        //public bool AlterarSenha { get; set; }
        public bool SenhaNuncaExpira { get; set; }
        public bool ContaDesabilitada { get; set; }
        public long DataExpiracao { get; set; }
    }



}


