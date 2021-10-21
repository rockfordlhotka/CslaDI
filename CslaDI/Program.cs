using System;
using System.Threading.Tasks;
using Csla;
using Microsoft.Extensions.DependencyInjection;

namespace CslaExtensions
{
  public static class Extensions
  {
    public static void AddCsla(this ServiceCollection services)
    {
      Csla.Configuration.ConfigurationExtensions.AddCsla(services);
      services.AddScoped(typeof(Csla.Core.IContextManager), typeof(Csla.Core.ApplicationContextManager));
      services.AddScoped<Csla.ApplicationContext>();

      services.AddSingleton(typeof(Csla.Server.IDataPortalServer), typeof(Csla.Server.DataPortal));
      services.AddSingleton(typeof(Csla.Server.Dashboard.IDashboard), typeof(Csla.Server.Dashboard.Dashboard));
      services.AddTransient<Csla.Server.DataPortalSelector>();
      services.AddTransient<Csla.Server.SimpleDataPortal>();
      services.AddTransient<Csla.Server.FactoryDataPortal>();
      services.AddTransient<Csla.Server.DataPortalBroker>();

      services.AddTransient(typeof(IDataPortal<>), typeof(Csla.DataPortal<>));
      services.AddTransient(typeof(IChildDataPortal<>), typeof(Csla.DataPortal<>));
    }
  }
}

namespace CslaDI
{
  using CslaExtensions;
  class Program
  {
    static async Task Main(string[] args)
    {
      ServiceCollection services = new();
      services.AddTransient<MainApp>();
      services.AddCsla();
      var provider = services.BuildServiceProvider();

      try
      {
        var app = provider.GetRequiredService<MainApp>();
        await app.Run();
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
        throw;
      }
    }
  }

  public class MainApp
  {
    public IDataPortal<PersonEdit> personPortal { get; private set; }
    public ApplicationContext ApplicationContext { get; private set; }
    public Csla.Server.Dashboard.IDashboard Dashboard { get; private set; }

    public MainApp(IDataPortal<PersonEdit> dataPortal, ApplicationContext applicationContext, Csla.Server.Dashboard.IDashboard dashboard)
    {
      personPortal = dataPortal;
      ApplicationContext = applicationContext;
      Dashboard = dashboard;
    }

    public async Task Run()
    {
      try
      {
        var person = await personPortal.CreateAsync("Andrea");
        WritePerson(person);

        person = await personPortal.FetchAsync("Andrea");
        WritePerson(person);

        person = await personPortal.CreateAsync("Andrea");
        person.Name = "Ali";
        await person.SaveAndMergeAsync();
        WritePerson(person);

        //person = await personPortal.FetchAsync("Boo");
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }

      await Task.Delay(500);
      Console.WriteLine();
      Console.WriteLine($"Data portal calls");
      Console.WriteLine($" - Completed:  {Dashboard.CompletedCalls}");
      Console.WriteLine($" - Failed:     {Dashboard.FailedCalls}");
      Console.WriteLine($" - First call: {Dashboard.FirstCall}");
      Console.WriteLine($" - Last call:  {Dashboard.LastCall}");
    }

    private void WritePerson(PersonEdit person)
    {
      Console.WriteLine($"Name {person.Name}, IsNew {person.IsNew}");
      Console.WriteLine($" Contacts {person.ContactList.Count}");
      foreach (var item in person.ContactList)
      {
        Console.WriteLine($" - {item.Type} {item.ContactInfo}");
      }
    }
  }

  [Serializable]
  public class PersonEdit : BusinessBase<PersonEdit>
  {
    public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(nameof(Name));
    public string Name
    {
      get => GetProperty(NameProperty);
      set => SetProperty(NameProperty, value);
    }

    public static readonly PropertyInfo<ContactEditList> ContactListProperty = RegisterProperty<ContactEditList>(nameof(ContactList));
    public ContactEditList ContactList
    {
      get => GetProperty(ContactListProperty);
      private set => LoadProperty(ContactListProperty, value);
    }

    [Create]
    private void Create(string name, [Inject] IChildDataPortal<ContactEditList> contactPortal)
    {
      Name = name;
      ContactList = contactPortal.CreateChild();
    }

    [Fetch]
    private void Fetch(string name, [Inject] IChildDataPortal<ContactEditList> contactPortal)
    {
      if (name == "Boo")
        throw new InvalidOperationException("Boo");

      using (BypassPropertyChecks)
      {
        Name = name;
      }
      ContactList = contactPortal.FetchChild();
    }

    [Insert]
    [Update]
    private void Update()
    {

    }
  }

  [Serializable]
  public class ContactEditList : BusinessListBase<ContactEditList, ContactEdit>
  {
    [CreateChild]
    private void Create()
    { }

    [FetchChild]
    private void Fetch([Inject] IChildDataPortal<ContactEdit> contactPortal)
    {
      using (LoadListMode)
      {
        Add(contactPortal.FetchChild("mobile", "555-1234"));
        Add(contactPortal.FetchChild("email", "someone@somewhere.foo"));
      }
    }
  }

  [Serializable]
  public class ContactEdit : BusinessBase<ContactEdit>
  {
    public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(nameof(Type));
    public string Type
    {
      get => GetProperty(NameProperty);
      set => SetProperty(NameProperty, value);
    }

    public static readonly PropertyInfo<string> ContactInfoProperty = RegisterProperty<string>(nameof(ContactInfo));
    public string ContactInfo
    {
      get => GetProperty(ContactInfoProperty);
      set => SetProperty(ContactInfoProperty, value);
    }

    [CreateChild]
    private void Create(string type, string info)
    {
      Type = type;
      ContactInfo = info;
    }

    [FetchChild]
    private void Fetch(string type, string info)
    {
      using (BypassPropertyChecks)
      {
        Type = type;
        ContactInfo = info;
      }
    }

    [InsertChild]
    private void Insert()
    {

    }

    [UpdateChild]
    private void Update()
    {

    }
    
    [DeleteSelfChild]
    private void DeleteSelf()
    { 
    
    }
  }
}
