using System.DirectoryServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace BrugerManager;

/// <summary>
/// Firmaets forskellige afdelinger
/// </summary>
public enum Role
{
    Marketing,
    WarehouseEmployee,
    PurchasingConsultant,
    WarehouseManager,
    PurchasingManager,
    SysAdmin,
    Director,
    Unknown,
}

public static class RoleExtension
{
    /// <summary>
    /// Bruges til at finde ud af hvilke afdeling medarbejderen tilhøre ud fra rolle (Lagermedarbejder, Lagerchef, ...)
    /// </summary>
    /// <param name="role">Navnet på medarbejderens rolle</param>
    /// <returns>En string med den afdeling de tilhøre</returns>
    public static string Group(this Role role)
    {
        return role switch
        {
            Role.Marketing => "Marketing",
            Role.WarehouseEmployee => "Lager Gruppen",
            Role.PurchasingConsultant => "Salg Group",
            Role.WarehouseManager => "Lager Gruppen",
            Role.PurchasingManager => "Salg Group",
            Role.SysAdmin => "System Admin",
            Role.Director => "Direktør og ejer",
            _ => "Unknown",
        };
    }


    /// <summary>
    /// Bruges til at konvertere afdeling som string om til afdeling enum
    /// </summary>
    /// <param name="role">Afdeling som string</param>
    /// <returns>Afdeling som Enum <see cref="Role"/></returns>
    public static Role FromString(string role)
    {
        return role switch
        {
            "Marketing" => Role.Marketing,
            "Lagermedarbejder" => Role.WarehouseEmployee,
            "Salgskonsulent" => Role.PurchasingConsultant,
            "Lagerchef" => Role.WarehouseManager,
            "Salgschef" => Role.PurchasingManager,
            "System Administrator" => Role.SysAdmin,
            "Direktør" => Role.Director,
            _ => Role.Unknown,
        };
    }
}

public class ActiveDirectoryHandler
{
    // Laver en LDAP connection object (protokollen AD er baseret på)
    // Reference: https://ianatkinson.net/computing/adcsharp.htm
    public DirectoryEntry connection = null;
    public string Domain;

    public ActiveDirectoryHandler(string domain)
    {
        this.Domain = domain;
    }

    /// <summary>
    /// Bruges til at check om der er forbindelse
    /// </summary>
    /// <returns>true hvis der er forbindelse ellers false</returns>
    public bool IsConnected()
    {
        return this.connection != null;
    }

    /// <summary>
    /// Bruges til at secure connect til det domæne AD tilhøre
    /// </summary>
    /// <param name="domain">Navnet på domænet</param>
    /// <returns>En string med fejl besked hvis der ikke kunne oprettes forbindelse</returns>
    public string? Connect(string domain)
    {
        // Forsøger at få en connection til domænet
        try
        {
            this.connection = new DirectoryEntry(domain);
            if (this.connection != null)
            {
                string[] domainSplitted = domain.Split(".");

                this.connection.Path = "LDAP://OU=" + domainSplitted[0];
                foreach (var dc in domainSplitted[1..])
                {
                    this.connection.Path += ",DC=" + dc;
                }

                // Hvis der er forbindelse til domæne, sættes det til at bruge secure authentication type
                this.connection.AuthenticationType = AuthenticationTypes.Secure;
            }
        }
        // Hvis der ikke kan oprettes forbindelse til domænet, kommer der en fejl string med Exception
        catch (Exception e)
        {
            return e.ToString();
        }

        this.Domain = domain;

        return null;
    }

    public string? Connect()
    {
        return this.Connect(this.Domain);
    }

    /// <summary>
    /// Bruges til at oprette OU til AD
    /// </summary>
    /// <param name="name">Navnet på OU der oprettes</param>
    /// <returns></returns>
    public string? AddGroup(string name)
    {
        this.connection.Parent.Children.Add("CN=" + name, "group");

        return null;
    }

    /// <summary>
    /// Bruges til at oprette medarbejder til AD og tilføje dem til afdelings OU
    /// </summary>
    /// <param name="user">Oplysninger på medarbejder der tilføjes</param>
    /// <returns>En fejl string med exception ved fejl under oprettelse</returns>
    public string? AddUser(User user)
    {
        /*
        // Opretter et user object der tilføles til AD
        // (CN = Common Name)
        */
        DirectoryEntry dirUser = this.connection.Children.Add("CN=" + user.FullName, "user");

        // Domæne baseret username ([Fornavn]@danskvinimport.local)
        dirUser.Properties["userprincipalname"].Add(user.Username + "@" + this.Domain);
        // Username (til ældre systemer)
        dirUser.Properties["samaccountname"].Add(user.Username);
        dirUser.Properties["sn"].Add(user.LastName); // Efternavn
        dirUser.Properties["givenname"].Add(user.FirstName); // Fornavn
        dirUser.Properties["displayname"].Add(user.FullName); // Username
        dirUser.Properties["description"].Add(user.Description); // Beskrivelse af medarbejder

        dirUser.CommitChanges(); // Gemmer medarbejders oplysninger til AD

        dirUser.Invoke("SetPassword", user.Password); // Sætter medarbejderens password

        // Tilføjer oprettet medarbejder til deres tilsvarende afdeling
        foreach (var group in user.Groups)
        {
            // TODO: Lav ny gruppe hvis gruppe ikke eksisterer
            // Leder efter eksisterende afdeling Group på AD
            DirectoryEntry dirGroup = this.connection.Parent.Children.Find("CN=" + group, "group");
            if (dirGroup != null)
            {
                // Hvis Group findes tilføjes medarbejder til Group
                dirGroup.Invoke("Add", new object[] { dirUser.Path.ToString() });
            }
        }

        dirUser.CommitChanges(); // Gemmer oplysninger til AD

        // Laver netværksmappen for brugeren
        Directory.CreateDirectory(user.NetworkDir);

        // Lav hjemme mappe ved at prøve at få adgang til den indtil den eksisterer, og så sætte den op.
        bool folderCreated = false;
        while (!folderCreated)
        {
            try
            {
                // Få ACL for brugerens netværksmappe
                // (Access Control List, returnerer listen af adgangsindstillinger for et objekt).
                DirectoryInfo dInfo = new DirectoryInfo(user.NetworkDir);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();

                // Sæt brugeren til at være mappens ejer
                IdentityReference newUser = new NTAccount(this.Domain + @"\" + user.Username);
                dSecurity.SetOwner(newUser);

                // Definer adgangsindstillingerne for mappen
                FileSystemAccessRule permissions =
                    new FileSystemAccessRule(newUser, FileSystemRights.FullControl,
                        AccessControlType.Allow);

                // Sæt de nye adgangsindstillinger
                dSecurity.AddAccessRule(permissions);
                dInfo.SetAccessControl(dSecurity);

                folderCreated = true;
            }

            catch (System.Security.Principal.IdentityNotMappedException)
            {
                // TODO: Do something!
                // Skip for now...
            }

            catch (Exception e)
            {
                return e.ToString();
            }
        }

        return null;
    }
}