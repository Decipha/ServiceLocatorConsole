using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Core.Network
{
    /// <summary>
    /// locates and controls windows services on remote machines within the same network.
    /// </summary>
    [Serializable]
    public class ServiceLocator
    {
        public ServiceLocator() { }

        public ServiceLocator(string machineName, string serviceName)
        {
            this.MachineName = machineName;
            this.ServiceName = serviceName;
            this.IPAddress   = Dns.GetHostEntry(this.MachineName).AddressList?.FirstOrDefault()?.ToString();
        }

        public ServiceLocator(ServiceController svc)
        {
            _svc = svc;

            this.MachineName = svc.MachineName;
            this.ServiceName = svc.ServiceName;
            this.IPAddress   = Dns.GetHostEntry(this.MachineName).AddressList?.FirstOrDefault()?.ToString();

        }

        public ServiceControllerStatus ServiceStatus
        {
            get
            {
                if (RemoteService != null)
                    return RemoteService.Status;
                else
                    return default(ServiceControllerStatus);
            }
        }


        public ServiceType ServiceType
        {
            get { if (RemoteService != null) return RemoteService.ServiceType; else return default(ServiceType); }
        }

        /*
        public ServiceStartMode StartupMode
        {
            get
            {
                if (RemoteService != null)
                    return RemoteService.StartType;
                else
                    return ServiceStartMode.Disabled;
            }
           
        }*/

        /// <summary>
        /// service-controller instance representing the service as located.
        /// </summary>
        ServiceController _svc;

        /// <summary>
        /// the name of the machine running the service.
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// the IP address of the machine.
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// the name of the service running on that machine.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// start the service if it is stopped.
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            var svc = RemoteService;
            if (svc != null)
            {
                if (svc.Status == ServiceControllerStatus.Stopped)
                {
                    svc.Start();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// stop the service if it is running.
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            var svc = RemoteService;
            if (svc != null)
            {
                if (svc.Status == ServiceControllerStatus.Running)
                {
                    svc.Stop();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// restarts the service
        /// </summary>
        /// <returns></returns>
        public bool Restart()
        {
            var svc = RemoteService;
            if (svc != null)
            {
                if (svc.Status == ServiceControllerStatus.Running)
                    svc.Stop();
                svc.WaitForStatus(ServiceControllerStatus.Stopped);
                if (svc.Status == ServiceControllerStatus.Stopped)
                    svc.Start();
                svc.WaitForStatus(ServiceControllerStatus.Running);
                return true;
            }
            return false;
        }

        /// <summary>
        /// executes a custom command against the service.
        /// </summary>
        /// <param name="cmd"></param>
        public void ExecuteCommand(int cmd)
        {
            var svc = RemoteService;
            if (svc != null)
            {
                svc.ExecuteCommand(cmd);
            }
            else
            {
                throw new ApplicationException($"Unable to execute custom command {cmd} on service {MachineName}.{ServiceName} - service not found");
            }
        }

        /// <summary>
        /// finds the service matching machine name and service name.
        /// </summary>
        /// <returns></returns>
        public ServiceController Find()
        {
            try
            {
                return (from s in ServiceController.GetServices(this.MachineName)
                        where s.ServiceName.Equals(this.ServiceName)
                        select s).FirstOrDefault();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// the remote service (or null if not found)
        /// </summary>
        [XmlIgnore]
        public ServiceController RemoteService
        {
            get
            {
                if (_svc == null)
                {
                    _svc = Find();
                }
                return _svc;
            }
        }

        /// <summary>
        /// does the service exist.
        /// </summary>
        [XmlIgnore]
        public bool Exists
        {
            get
            {
                if (_svc != null)
                    return true;
                try
                {
                    var services = ServiceController.GetServices(this.MachineName);
                    if (services.Length > 0)
                    {
                        foreach (var srv in services)
                        {
                            if (srv.MachineName.Equals(this.MachineName))
                            {
                                return true;
                            }
                        }
                    }
                }
                catch { }

                return false;
            }
        }

        /// <summary>
        /// is the service running?
        /// </summary>
        [XmlIgnore]
        public bool IsRunning
        {
            get
            {
                if (_svc != null && _svc.Status == ServiceControllerStatus.Running)
                    return true;
                try
                {
                    var services = ServiceController.GetServices(this.MachineName);
                    if (services.Length > 0)
                    {
                        foreach (var srv in services)
                        {
                            if (srv.MachineName.Equals(this.MachineName))
                            {
                                if (srv.Status == ServiceControllerStatus.Running)
                                    return true;
                            }
                        }
                    }

                }
                catch
                {

                }
                return false;
            }
        }

        /// <summary>
        /// enumerates local services.
        /// </summary>
        [XmlIgnore]
        public static IEnumerable<ServiceLocator> LocalServices
        {
            get
            {
                foreach (var svc in ServiceController.GetServices())
                {
                    yield return new ServiceLocator(svc);
                }
            }
        }

        /// <summary>
        /// enumerates services from a particular machine
        /// </summary>
        /// <param name="machineName"></param>
        /// <returns></returns>
        public static IEnumerable<ServiceLocator> GetServices(string machineName)
        {
            ServiceController[] services = null;
            try
            {
                services = ServiceController.GetServices(machineName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            if (services != null)
            {
                foreach (var svc in services)
                {
                    yield return new ServiceLocator(svc);
                }
            }

        }

        /// <summary>
        /// finds all the services visible from the current network location.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<ServiceLocator> FindServices()
        {
            // start net-view
            var local = new NetViewWrapper();

            // wait for the network search to complete:
            local.Process.Wait();

            // now enumerate the machines:
            foreach (var computerName in local.ComputerNames)
            {
                var services = default(IEnumerable<ServiceLocator>);
                try
                {
                    services = GetServices(computerName);
                }
                catch (Exception accessException)
                {
                    // skip this machine;
                    Console.WriteLine($"Unable to Access Service-Manager on {computerName}: {accessException.Message}");
                }
                if (services != null)
                {
                    foreach (var svc in services)
                    {
                        yield return svc;
                    }
                }
            }
        }

    }

}
