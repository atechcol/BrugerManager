using System.Security;

namespace BrugerManager;

// Record fordi vi gerne vil have en struktur der fungerer som en primitive.
public abstract record StringResult;

public record SucceededStringResult(string Value) : StringResult;

public record FailedStringResult(Exception Why) : StringResult;

/// <summary>
/// Bruges tilve UI til user input
/// </summary>
public class UI
{
    private const string Prompt = ">> ";
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
        Console.Write(UI.Prompt);

        string? inputMaybe = Console.ReadLine();
        string input = inputMaybe switch
        {
            null => ":null",
            _ => inputMaybe,
        };

        this.Log.Add(input);

        return new SucceededStringResult(input);
    }

    public StringResult ReadLineForced()
    {
        Console.Write(UI.Prompt);

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

    /// <summary>
    /// UI og input til oprettelse af medarbejder
    /// </summary>
    public void CreateUser()
    {
        Info("Fornavn:");
        SucceededStringResult firstName = ReadLine();
        Info("Efternavn:");
        SucceededStringResult lastName = ReadLine();
        Info("Beskrivelse:");
        SucceededStringResult description = ReadLine();
        Info("Rolle:");
        SucceededStringResult role = ReadLine();
        Info("Password:");
        SucceededStringResult password = ReadLine();
        Info("Grupper (kommasepareret):");
        SucceededStringResult groups = ReadLine();
        
        // Hvis der er mellemrum, så fjern dem, og så split på kommaer
        string[] splittedGroups = groups.Value.Replace(" ", string.Empty).Split(",");

        User user = new User(
            firstName: firstName.Value,
            lastName: lastName.Value,
            description: description.Value,
            role: RolesExtension.FromString(role.Value),
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