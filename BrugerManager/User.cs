using System.Security;

namespace BrugerManager;

/// <summary>
/// Class der bruges til at oprette medarbejder med nødvendige information
/// </summary>
public class User
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Description { get; set; }
    public Role Role { get; set; }
    public object[] Password { get; }
    public string[] Groups { get; set; }
    public string Username => FirstName;
    public string FullName => FirstName + " " + LastName;
    public string NetworkDrive { get; set; }
    public string NetworkDir { get; set; }

    public User(string firstName, string lastName, string description, Role role, string password, string[] groups)
    {
        this.FirstName = firstName;
        this.LastName = lastName;
        this.Description = description;
        this.Role = role;
        this.Password = (object[]) [password];
        this.Groups = groups;
        this.NetworkDrive = "Z:";
        this.NetworkDir = @"Z:\\TEAMJOHN\Bruger\" + this.Username;
    }

    public User(string firstName, string lastName, string description, Role role, string password,
        string[] groups, string homeDrive, string homeDir)
    {
        FirstName = firstName;
        LastName = lastName;
        Description = description;
        Role = role;
        Password = (object[]) [password];
        Groups = groups;
        NetworkDrive = homeDrive;
        NetworkDir = homeDir;
    }
}