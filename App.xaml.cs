using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.Logging;
using NLog;
using Prism.Events;
using Prism.Ioc;
using Prism.Unity;

using StudentSeating.Data;
using StudentSeating.Interfaces;

namespace StudentSeating
{
    public partial class App : PrismApplication
    {
        public class AppClosing : PubSubEvent<Boolean> { }

        protected override Window CreateShell()
        {
            Console.WriteLine("Create Shell " + DateTime.Now);
            var w = Container.Resolve<Views.SeatingMain>();

            return w;
        }

        /// <summary>
        /// Wiring up the dependency injector, Prism, used to facilitate communication
        /// </summary>
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IOptionStore, Models.OptionStore>();
            //containerRegistry.RegisterSingleton<IAppointmentStore, Models.AppointmentStore>();
            containerRegistry.RegisterSingleton(typeof(ViewModels.SeatingMainViewModel));
            containerRegistry.RegisterSingleton(typeof(Data.ExamExcelReader));

            string context_path = "";
            string options_path = "";
            string dbs_password = "";

            SeatingContext context;
            OptionsContext optionsContext;
            try
            {
                string json = File.ReadAllText(@"C:\Users\Thrym\Desktop\storage.json");
                JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;
                root.TryGetProperty("databases", out JsonElement dbs);
                dbs.TryGetProperty("seating", out JsonElement seating);
                dbs.TryGetProperty("options", out JsonElement options);

                context_path = seating.GetProperty("path").GetString();
                options_path = options.GetProperty("path").GetString();

                dbs_password = seating.GetProperty("key").GetString();
                
                context = new SeatingContext()
                {
                    DbPath = context_path,
                    DbKey = dbs_password
                };

                context.Database.EnsureCreated();

                containerRegistry.RegisterInstance(context);

                optionsContext = new OptionsContext()
                {
                    DbPath = options_path,
                    DbKey = dbs_password
                };

                //optionContext.Database.EnsureCreated();

                containerRegistry.RegisterInstance(optionsContext);
            }
            catch (FileNotFoundException fnfe)
            {
                Logger logger = LogManager.GetLogger("Startup.Error");
                logger.Error(fnfe, "Unable to find a data related file.");
            }
            catch (DirectoryNotFoundException dnfe)
            {
                Logger logger = LogManager.GetLogger("Startup.Error");
                logger.Error(dnfe, "Unable to find S-Drive");
            }
            catch (KeyNotFoundException knfe)
            {
                Logger logger = LogManager.GetLogger("Startup.Error");
                logger.Error(knfe, "Invalid JSON keys. \"path\" and \"key\" expected.");
            }
            catch (JsonException je)
            {
                Logger logger = LogManager.GetLogger("Startup.Error");
                logger.Error(je, "Check the Seating.JSON file for issues.");
            }
            catch (Exception e)
            {
                Logger logger = LogManager.GetLogger("Startup.Error");
                logger.Error(e, "Database Wiring Issue");
            }

            // Global cancellation token. If close of the app is called for, this should (**assumption**)
            //  signal the various pieces it's passed to.
            CancellationTokenSource _cancel = new CancellationTokenSource();
            containerRegistry.RegisterInstance(_cancel);

            // Register the tying of the Views as Dialog windows
            containerRegistry.RegisterDialog<Views.EmptySeat, ViewModels.EmptySeatViewModel>();
            containerRegistry.RegisterDialog<Views.SeatedExam, ViewModels.SeatedExamViewModel>();
            containerRegistry.RegisterDialog<Views.FindSeat, ViewModels.FindSeatViewModel>();
        }
    }
}
