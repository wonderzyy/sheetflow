
namespace OOP_Lab1.Models;

public class Cell
{
    public string Address { get; }
    public string Expression { get; set; } = string.Empty;
    public string? Error { get; set; }

    public Cell(string address)
    {
        Address = address;
    }
}
