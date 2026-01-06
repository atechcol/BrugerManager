namespace BrugerManager;

class Program
{
    static void Main(string[] args)
    {
        ActiveDirectoryHandler activeDirectoryHandler = new ActiveDirectoryHandler("LDAP://danskvinimport.local/DC=danskvimimport,DC=local", "", "");
        activeDirectoryHandler.Connect();
        UI ui = new UI(activeDirectoryHandler);
        ui.CreateUser();
    }
}
