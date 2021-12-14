using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

using StudentSeating.Interfaces;
using StudentSeating.Data;

namespace StudentSeating.Models
{
    public class OptionStore : IOptionStore, INotifyPropertyChanged
    {
        public new event PropertyChangedEventHandler PropertyChanged;

        public int SeatingUpdateRate
        {
            get
            {
                bool found = Options.TryGetValue("SeatUpdateRate", out string value);
                return found ? int.Parse(value) : 1;
            }
            set
            {
                changeMade("SeatUpdateRate", value.ToString());
            }
        }
        public int ExamUpdateRate
        {
            get
            {
                bool found = Options.TryGetValue("ExamUpdateRate", out string value);
                return found ? int.Parse(value) : 2;
            }
            set
            {
                changeMade("ExamUpdateRate", value.ToString());
            }
        }
        public int OptionUpdateRate
        {
            get
            {
                bool found = Options.TryGetValue("OptionUpdateRate", out string value);
                return found ? int.Parse(value) : 5;
            }
            set
            {
                changeMade("OptionUpdateRate", value.ToString());
            }
        }

        public int WindowWidth
        {
            get
            {
                bool found = Options.TryGetValue("WindowWidth", out string value);
                return found ? int.Parse(value) : 500;
            }
            set
            {
                changeMade("WindowWidth", value.ToString());
            }
        }
        public int WindowHeight
        {
            get
            {
                bool found = Options.TryGetValue("WindowHeight", out string value);
                return found ? int.Parse(value) : 500;
            }
            set
            {
                changeMade("WindowWidth", value.ToString());
            }
        }
        public double FontSize
        {
            get
            {
                bool found = Options.TryGetValue("FontSize", out string value);
                return found ? int.Parse(value) : 15;
            }
            set
            {
                changeMade("WindowWidth", value.ToString());
            }
        }
        public string ExamFilePath
        {
            get
            {
                bool found = Options.TryGetValue("ExamFilePath", out string value);
                return found ? value : @".\Testing Center Log - Current";
            }
            set
            {
                changeMade("WindowWidth", value);
            }
        }
        public int ConnectionRetries
        {
            get
            {
                bool found = Options.TryGetValue("ConnectionRetries", out string value);
                return found ? int.Parse(value) : 3;
            }
            set
            {
                changeMade("WindowWidth", value.ToString());
            }
        }
        public int SeatSpacing
        {
            get
            {
                bool found = Options.TryGetValue("SeatSpacing", out string value);
                return found ? int.Parse(value) : 2;
            }
            set
            {
                changeMade("WindowWidth", value.ToString());
            }
        }

        public Action OptionMonitoring
        {
            get; set;
        }

        private Dictionary<string, DateTime> changedOptions;
        private Dictionary<string, string> Options;
        private OptionsContext _context;
        public OptionStore(OptionsContext context)
        {
            string json = File.ReadAllText(@"C:\Users\Thrym\Desktop\storage.json");
            JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            root.TryGetProperty("databases", out JsonElement dbs);
            dbs.TryGetProperty("options", out JsonElement exams);

            this._context = context;
            this._context.DbPath = exams.GetProperty("path").GetString();
            this._context.DbKey = exams.GetProperty("key").GetString();

            this._context.Database.EnsureCreated();
            this._context.Database.CanConnect();

            Options = new Dictionary<string, string>();
            changedOptions = new Dictionary<string, DateTime>();
            LoadOptions();
        }

        public Task Close()
        {
            SaveOptions();
            this._context.SaveChangesAsync();

            return Task.CompletedTask;
        }

        public void LoadOptions()
        {
            foreach (Option options in this._context.Options)
            {
                this.Options.Add(options.Id, options.Value);
            }
        }

        public void SaveOptions()
        {
            Option o = null;
            foreach (KeyValuePair<string,DateTime> kvp in changedOptions)
            {
                o = this._context.Options.Find(kvp.Key);
                if (o == null)
                {
                    this._context.Options.Add(new Option() { Id = kvp.Key, Value = this.Options[kvp.Key], LastUpdated = kvp.Value });
                }
                else if (o.LastUpdated > kvp.Value)
                {
                    o.Value = this.Options[kvp.Key];
                    this._context.Entry(o).State = EntityState.Modified;
                }
            }

            changedOptions.Clear();
        }

        public void StartOptionUpdate(Action callback = null)
        {
            Task.Run(() =>
            {
                //This is primarily to load in initial values for options so that the store has values already loaded
                // so that the UI has the settings already available
                int count = this._context.Options.Count();

                //I may also be able to, at this point, wire up an event for the edge-cases related to exam log path and seating size.
                // Or, just pass the optionContext to the other two contexts? 
                // Food for thought

            });
        }

        private void changeMade(string key, string value)
        {
            if (this.Options.ContainsKey(key))
            {
                this.Options[key] = value;
            }
            else
            {
                this.Options.Add(key, value);
            }

            if (this.changedOptions.ContainsKey(key))
            {
                this.changedOptions[key] = DateTime.Now;
            }
            else
            {
                this.changedOptions.Add(key, DateTime.Now);
            }

            NotifyPropertyChanged(key);
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
