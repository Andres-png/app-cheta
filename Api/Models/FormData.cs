namespace Api.Models;

public class FormData
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Nombre { get; set; }
    public required string Email { get; set; }
    public string Telefono { get; set; } = "";
    public string Nota { get; set; } = "";
}
