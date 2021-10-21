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
      services.AddSingleton(typeof(Csla.Core.IContextManager), typeof(Csla.Core.ApplicationContextManager));
      services.AddSingleton<Csla.ApplicationContext>();
      services.AddSingleton(typeof(Csla.Server.IDataPortalServer), typeof(Csla.Server.DataPortal));
      services.AddSingleton(typeof(Csla.Server.Dashboard.IDashboard), typeof(Csla.Server.Dashboard.Dashboard));
      services.AddTransient<Csla.Server.SimpleDataPortal>();
      services.AddTransient<Csla.Server.FactoryDataPortal>();
      services.AddTransient<Csla.Server.DataPortalSelector>();
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
    public MainApp(IDataPortal<PersonEdit> dataPortal, ApplicationContext applicationContext)
    {
      personPortal = dataPortal;
      ApplicationContext = applicationContext;
    }

    public async Task Run()
    {
      try
      {
        var person = await personPortal.CreateAsync("Andrea");
        Console.WriteLine($"Name {person.Name}, IsNew {person.IsNew}");
        person = await personPortal.FetchAsync("Andrea");
        Console.WriteLine($"Name   {person.Name}, IsNew {person.IsNew}");
        Console.WriteLine($"Mobile {person.ContactList[0].ContactInfo}");
        person = await personPortal.CreateAsync("Andrea");
        person.Name = "Ali";
        await person.SaveAndMergeAsync();
        Console.WriteLine($"Name {person.Name}, IsNew {person.IsNew}");
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
        throw;
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
    [FetchChild]
    private void Fetch([Inject] IChildDataPortal<ContactEdit> contactPortal)
    {
      using (LoadListMode)
      {
        Add(contactPortal.FetchChild("mobile", "555-1234"));
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
    [UpdateChild]
    private void Update()
    {

    }
    
    [DeleteSelfChild]
    private void Delete()
    { 
    
    }
  }
}
