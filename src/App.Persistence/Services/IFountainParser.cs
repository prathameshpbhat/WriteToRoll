using App.Core.Models;

namespace App.Persistence.Services
{
    public interface IFountainParser
    {
        Script ParseFountain(string fountainText);
        string ConvertToFountain(Script script);
        bool ValidateFountain(string fountainText);
    }
}
