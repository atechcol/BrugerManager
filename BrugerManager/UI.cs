using System.Security;

namespace BrugerManager;

// Vi definerer nogle strukturer der viser, om det string-resultat vi får
// fra ReadLine er gode eller dårlige. 
public abstract record StringResult;

public record SucceededStringResult(string Value) : StringResult;

public record FailedStringResult(Exception Why) : StringResult;

/// <summary>
/// Bruges tilve UI til user input
/// </summary>
public class UI
{
    private const string PromptHeader = ">> ";
    private List<string> Log { get; }

    private ActiveDirectoryHandler _adHandler;

    public UI(ActiveDirectoryHandler adHandler)
    {
        _adHandler = adHandler;
        this.Log = new List<string>();
        
        Info("Brugermanager v0.0.1-ALPHA -- danskvinimport.local");
    }

    /// <summary>
    /// Printer normal tekst til brugeren 
    /// </summary>
    /// <param name="message">Beskeden der skal printes</param>
    public void Info(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// Printer fejl til brugeren
    /// </summary>
    /// <param name="error">Fejlen der skal printes</param>
    public void Error(string error)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(error);
        Console.ResetColor();
    }

    /// <summary>
    /// Læser brugerens input
    /// </summary>
    /// <returns></returns>
    public SucceededStringResult ReadLine()
    {
        Console.Write(UI.PromptHeader);

        string? inputMaybe = Console.ReadLine();
        string input = inputMaybe switch
        {
            null => ":null",
            _ => inputMaybe,
        };

        this.Log.Add(input);

        return new SucceededStringResult(input);
    }

    public StringResult ReadLineNonEmpty()
    {
        Console.Write(UI.PromptHeader);

        string? inputMaybe = Console.ReadLine();
        switch (inputMaybe)
        {
            case null:
                return new FailedStringResult(new Exception("Input må ikke være tom!"));
            default:
                this.Log.Add(inputMaybe);
                return new SucceededStringResult(inputMaybe);
        }
    }

    public SucceededStringResult Prompt(string message)
    {
        Info(message);
        return ReadLine();
    }

    public StringResult PromptNonEmpty(string message)
    {
        Info(message);
        return ReadLineNonEmpty();
    }

    /// <summary>
    /// UI og input til oprettelse af medarbejder
    /// </summary>
    public void CreateUser()
    {
        SucceededStringResult firstName = Prompt("Fornavn:");
        SucceededStringResult lastName = Prompt("Efternavn:");
        SucceededStringResult description = Prompt("Beskrivelse:");
        SucceededStringResult role = Prompt("Rolle:");
        SucceededStringResult password = Prompt("Password:");
        SucceededStringResult groups = Prompt("Grupper (kommasepareret):");
        
        // Hvis der er mellemrum, så fjern dem, og så split på kommaer
        string[] splittedGroups = groups.Value.Replace(" ", string.Empty).Split(",");

        User user = new User(
            firstName: firstName.Value,
            lastName: lastName.Value,
            description: description.Value,
            role: RoleExtension.FromString(role.Value),
            password: password.Value,
            groups: splittedGroups
        );

        string? result = _adHandler.AddUser(user);
        
        // Hvis vi får en besked er noget gået galt.
        if (result != null)
        {
            Error(result);
        }

        Info("Bruger ");
    }
}