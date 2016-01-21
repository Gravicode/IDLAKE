using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// This library can be used for IOC Container
/// </summary>
namespace IDLake.Web
{
    public class LibraryContainer
    {
        private readonly Dictionary<string, object> configuration
                       = new Dictionary<string, object>();
        private readonly Dictionary<Type, object> typeToCreator
                       = new Dictionary<Type, object>();
     
        public dynamic this[Type t]
        {
            get
            {
                return typeToCreator[t];
            }
        }
        public Dictionary<string, object> Configuration
        {
            get { return configuration; }
        }

        public T Get<T>()
        {
            return (T)typeToCreator[typeof(T)];
        }

        public void RegisterLibrary<T>(object creator)
        {
            typeToCreator.Add(typeof(T), creator);

        }
        
        public T GetConfiguration<T>(string name)
        {
            return (T)configuration[name];
        }
    }
}
//sample how to use
/*
 LibraryContainer container = new LibraryContainer();
//registering dependecies
container.Register<IRepository>(delegate
{
	return new NHibernateRepository();
});
container.Configuration["email.sender.port"] = 1234;
container.Register<IEmailSender>(delegate
{
	return new SmtpEmailSender(container.GetConfiguration<int>("email.sender.port"));
});
container.Register<LoginController>(delegate
{
	return new LoginController(
		container.Create<IRepository>(),
		container.Create<IEmailSender>());
});

//using the container
Console.WriteLine(
	container.Create<LoginController>().EmailSender.Port
	);
*/