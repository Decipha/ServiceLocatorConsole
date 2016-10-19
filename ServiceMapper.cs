using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Network
{
    /// <summary>
    /// generates a list of machines and the services running on them from the network.
    /// </summary>
    public class ServiceMapper
    {
        /// <summary>
        /// maps the windows services visible from the current network location.
        /// </summary>
        public static void Map()
        {
            StoreMap(QueryWindowsNetworkServices());
        }

        /// <summary>
        /// enumerates all services on all machines.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<MapRecord> QueryWindowsNetworkServices()
        {
            // start the net wrapper:
            var network = new NetViewWrapper();

            // wait for the process to find network computers:
            network.Process.Wait();

            Console.WriteLine($"Found {network.ComputerNames.Count} computers in local network neighborhood");

            // holder for output record
            List<MapRecord> maps = new List<MapRecord>();

            // enumerate those machine-names found on the local network
            Parallel.ForEach(network.ComputerNames, (machine) =>
            {
                // enumerate the services:
                Parallel.ForEach(ServiceLocator.GetServices(machine), (service) =>
                {

                    // push the machine and service name to the console for updates:
                    Console.WriteLine($"{machine}.{service.ServiceName}");

                    try
                    {
                        // try to create the map record:
                        maps.Add(MapRecord.Create(service));
                    }
                    catch (Exception create)
                    {
                        // write some exception details to the console:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error Accessing Services on {machine}");
                        Console.WriteLine(create.Message);
                        var trace = new System.Diagnostics.StackTrace(create);
                        foreach (var sf in trace.GetFrames())
                        {
                            Console.WriteLine($"{sf.GetMethod().Name} {System.IO.Path.GetFileName(sf.GetFileName())} {sf.GetFileLineNumber()}");
                        }
                        Console.ForegroundColor = ConsoleColor.White;
                    }

                });
            });

            return maps;
        }

        /// <summary>
        /// write the data line by line into a CSV.
        /// </summary>
        /// <param name="recs">
        /// the query enumerating the networked computers.
        /// </param>
        public static void StoreMap(IEnumerable<MapRecord> recs)
        {
            using (var fs = System.IO.File.OpenWrite(System.IO.Path.Combine(Environment.CurrentDirectory, "windowsNetworkServices.csv")))
            {
                var writer = new System.IO.StreamWriter(fs);
                int count = 0;

                foreach (var record in recs.OrderBy((r)=>r.MachineName)) 
                {
                    if (count++ == 0)
                        writer.WriteLine(record.GetDelimitedHeadRow(','));

                    writer.WriteLine(record.GetDelimitedTextRow(','));
                }
                writer.Close();
                writer.Dispose();
            }
        }

        public static void PersistServiceMap(IEnumerable<MapRecord> map)
        {
            using (var fs = System.IO.File.OpenWrite(FileNameXml))
            {
                new System.Xml.Serialization.XmlSerializer(typeof(MapRecord[])).Serialize(fs, map.ToArray());
            }
        }

        public static string FileNameXml
        {
            get
            {
                return System.IO.Path.Combine(Environment.CurrentDirectory, "serviceMap.xml");
            }
        }
    }

    [Serializable]
    public class MapRecord
    {
        public MapRecord() { }

        public string ServiceName { get; set; }
        public string MachineName { get; set; }
        public string DisplayName { get; set; }
        public string IPAddress   { get; set; }

        public string Type { get; set; }
        public string State { get; set; }
        public string StartMode { get; set; }
        public string DependentServices { get; set; }


        public static MapRecord Create(ServiceLocator source)
        {
            var sb = new StringBuilder();

            foreach (var d in source.RemoteService.DependentServices)
            {
                try
                {
                    if (sb.Length > 0)
                        sb.Append('|');
                    sb.Append(d.DisplayName);
                }
                catch { }
            }

            return new MapRecord
            {
                MachineName = source.MachineName,
                ServiceName = source.ServiceName,
                DisplayName = source.RemoteService.DisplayName,
                IPAddress   = source.IPAddress,
                Type        = source.RemoteService.ServiceType.ToString(),
                State       = source.RemoteService.Status.ToString(),
                DependentServices = sb.ToString()
            };
        }

        public string GetDelimitedTextRow(char colDelimiter)
        {
            var props = this.GetType().GetProperties();
            var sb = new StringBuilder();

            foreach (var prp in props)
            {
                if (sb.Length > 0)
                    sb.Append(colDelimiter);
                sb.Append(prp.GetValue(this));
            }

            return sb.ToString();
        }

        public string GetDelimitedHeadRow(char colDelimiter)
        {
            var props = this.GetType().GetProperties();
            var sb = new StringBuilder();

            foreach (var prp in props)
            {
                if (sb.Length > 0)
                    sb.Append(colDelimiter);
                sb.Append(prp.Name);
            }

            return sb.ToString();
        }
    }
}
